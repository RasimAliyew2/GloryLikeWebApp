using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerCandidatesController : Controller
{
    private readonly IEmployerCandidateMessagingApiService _apiService;

    public EmployerCandidatesController(
        IEmployerCandidateMessagingApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet("/Employer/Candidates/{candidateUserId:int}")]
    public async Task<IActionResult> Profile(
        int candidateUserId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Challenge();

        var model = new EmployerCandidatePageViewModel
        {
            ActorUserId = actorUserId,
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };

        var result = await _apiService.GetCandidateProfileAsync(
            actorUserId,
            candidateUserId,
            cancellationToken);

        if (!result.Success
            || result.Data is null
            || result.Data.Candidate is null)
        {
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Candidate profili yüklənmədi."
                : result.Message;
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("Profile", model);
        }

        result.Data.Candidate.Skills ??= [];
        result.Data.Candidate.Experiences ??= [];
        result.Data.Candidate.VacancyHistory ??= [];
        result.Data.Candidate.TeamMembers ??= [];
        model.Candidate = result.Data.Candidate;

        return View("Profile", model);
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
