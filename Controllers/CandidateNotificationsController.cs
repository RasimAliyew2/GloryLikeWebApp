using System.Security.Claims;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployeePolicy)]
public sealed class CandidateNotificationsController : Controller
{
    private readonly IVacancyApiService _vacancyApiService;

    public CandidateNotificationsController(
        IVacancyApiService vacancyApiService)
    {
        _vacancyApiService = vacancyApiService;
    }

    [HttpGet("/Candidate/Notifications")]
    public async Task<IActionResult> List(
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Unauthorized(new { success = false });

        var result = await _vacancyApiService.GetCandidateNotificationsAsync(
            candidateUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { success = false, message = result.Message });
        }

        return Ok(new
        {
            success = true,
            unreadCount = result.Data.UnreadCount,
            notifications = result.Data.Notifications.Select(notification => new
            {
                notificationId = notification.NotificationId,
                vacancyId = notification.VacancyId,
                applicationId = notification.ApplicationId,
                title = notification.Title,
                message = notification.Message,
                isRead = notification.IsRead,
                createdAtUtc = notification.CreatedAtUtc,
                targetUrl = Url.Action(
                    "Details",
                    "Applications",
                    new { vacancyId = notification.VacancyId })
            })
        });
    }

    [HttpPost("/Candidate/Notifications/{notificationId:long}/Read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(
        long notificationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Unauthorized(new { success = false });

        var result = await _vacancyApiService.MarkCandidateNotificationReadAsync(
            candidateUserId,
            notificationId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            redirectUrl = Url.Action(
                "Details",
                "Applications",
                new { vacancyId = result.Data.VacancyId })
        });
    }

    private bool TryGetCandidateUserId(out int candidateUserId) =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out candidateUserId)
        && candidateUserId > 0;
}
