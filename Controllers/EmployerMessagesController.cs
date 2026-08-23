using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerMessagesController : Controller
{
    private readonly IEmployerCandidateMessagingApiService _apiService;

    public EmployerMessagesController(
        IEmployerCandidateMessagingApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet("/Employer/Messages")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Challenge();

        var model = new EmployerMessagesPageViewModel
        {
            ActorUserId = actorUserId,
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };

        var result = await _apiService.GetOverviewAsync(actorUserId, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Company mesajları yüklənmədi."
                : result.Message;
        }
        else
        {
            result.Data.TeamMembers ??= [];
            result.Data.Conversations ??= [];
            model.Overview = result.Data;
        }

        return View("Messages", model);
    }

    [HttpGet("/Employer/Messages/UnreadCount")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var result = await _apiService.GetUnreadCountAsync(actorUserId, cancellationToken);
        if (!result.Success || result.Data is null)
            return StatusCode(StatusCodes.Status502BadGateway, new { message = result.Message });

        return Ok(new { unreadCount = Math.Max(0, result.Data.UnreadCount) });
    }

    [HttpGet("/Employer/Messages/Thread")]
    public async Task<IActionResult> Thread(
        [FromQuery] int otherUserId,
        [FromQuery] int candidateUserId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var result = await _apiService.GetThreadAsync(
            actorUserId,
            otherUserId,
            candidateUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
            return BadRequest(new { message = result.Message });

        result.Data.Messages ??= [];
        return Ok(result.Data);
    }

    [HttpPost("/Employer/Messages/Send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        [FromBody] SendCompanyCandidateMessageInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        if (input.RecipientUserId <= 0
            || input.CandidateUserId <= 0
            || string.IsNullOrWhiteSpace(input.Body))
        {
            return BadRequest(new { message = "Recipient, candidate və mesaj tələb olunur." });
        }

        var result = await _apiService.SendAsync(actorUserId, input, cancellationToken);
        if (!result.Success || result.Data is null)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpPost("/Employer/Messages/Read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(
        [FromBody] MarkCompanyMessageThreadReadInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var result = await _apiService.MarkReadAsync(actorUserId, input, cancellationToken);
        if (!result.Success || result.Data is null)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    private bool TryGetActorUserId(out int actorUserId) =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out actorUserId)
        && actorUserId > 0;

    private string GetDisplayName()
    {
        var value = string.Join(
            " ",
            new[]
            {
                User.FindFirstValue(ClaimTypes.Name),
                User.FindFirstValue(ClaimTypes.Surname)
            }.Where(item => !string.IsNullOrWhiteSpace(item)));

        return string.IsNullOrWhiteSpace(value)
            ? User.FindFirstValue("username") ?? "Employer"
            : value;
    }
}
