using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployeePolicy)]
public sealed class ApplicationsController : Controller
{
    private readonly IVacancyApiService _vacancyApiService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        IVacancyApiService vacancyApiService,
        ILogger<ApplicationsController> logger)
    {
        _vacancyApiService = vacancyApiService;
        _logger = logger;
    }

    [HttpGet("/Applications")]
    public async Task<IActionResult> Index(
        int? vacancyId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Challenge();

        var model = new CandidateApplicationsViewModel
        {
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            HighlightVacancyId = vacancyId
        };

        var result = await _vacancyApiService.GetCandidateApplicationsAsync(
            candidateUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Applications could not be loaded."
                : result.Message;
            _logger.LogWarning(
                "Candidate {CandidateUserId} applications failed: {Message}",
                candidateUserId,
                model.ErrorMessage);
            return View(model);
        }

        model.Applications = result.Data.Applications
            .OrderByDescending(application => application.AppliedAtUtc)
            .Select(application => new CandidateApplicationViewItem
            {
                ApplicationId = application.ApplicationId,
                VacancyId = application.VacancyId,
                PlatformVacancyId = application.PlatformVacancyId,
                CompanyName = string.IsNullOrWhiteSpace(application.CompanyName)
                    ? "Employer"
                    : application.CompanyName.Trim(),
                RoleTitle = string.IsNullOrWhiteSpace(application.RoleTitle)
                    ? string.IsNullOrWhiteSpace(application.PositionName)
                        ? $"Vacancy #{application.VacancyId}"
                        : application.PositionName.Trim()
                    : application.RoleTitle.Trim(),
                LocationName = application.LocationName,
                EmploymentType = application.EmploymentType,
                VacancyStatus = application.VacancyStatus ?? string.Empty,
                ApplicationStatus = application.ApplicationStatus
                    ?? string.Empty,
                FunnelStageName = string.IsNullOrWhiteSpace(application.FunnelStageName)
                    ? "Applied"
                    : application.FunnelStageName.Trim(),
                FunnelStageIndex = application.FunnelStageIndex,
                FunnelStageCount = application.FunnelStageCount,
                AppliedAtUtc = application.AppliedAtUtc,
                FunnelStageUpdatedAtUtc = application.FunnelStageUpdatedAtUtc,
                HiredAtUtc = application.HiredAtUtc
            })
            .ToList();

        return View(model);
    }

    private bool TryGetCandidateUserId(out int candidateUserId) =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out candidateUserId)
        && candidateUserId > 0;

    private string GetDisplayName()
    {
        var displayName = string.Join(
            " ",
            new[]
            {
                User.FindFirstValue(ClaimTypes.Name),
                User.FindFirstValue(ClaimTypes.Surname)
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(displayName)
            ? User.FindFirstValue("username") ?? "Candidate"
            : displayName;
    }
}
