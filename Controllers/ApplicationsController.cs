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
            .Select(MapApplication)
            .ToList();

        return View(model);
    }

    [HttpGet("/Applications/{vacancyId:int}")]
    public async Task<IActionResult> Details(
        int vacancyId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Challenge();

        var model = new CandidateApplicationDetailsViewModel
        {
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };

        var result = await _vacancyApiService.GetCandidateApplicationsAsync(
            candidateUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Application could not be loaded."
                : result.Message;
            return View(model);
        }

        var application = result.Data.Applications.FirstOrDefault(item =>
            item.VacancyId == vacancyId);

        if (application is null)
        {
            model.ErrorMessage =
                "Application was not found or does not belong to this candidate.";
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View(model);
        }

        model.Application = MapApplication(application);
        return View(model);
    }

    private static CandidateApplicationViewItem MapApplication(
        CandidateApplicationApiItem application)
    {
        return new CandidateApplicationViewItem
        {
            ApplicationId = application.ApplicationId,
            VacancyId = application.VacancyId,
            CompanyOwnerUserId = application.CompanyOwnerUserId,
            PlatformVacancyId = application.PlatformVacancyId,
            CompanyName = string.IsNullOrWhiteSpace(application.CompanyName)
                ? "Employer"
                : application.CompanyName.Trim(),
            RoleTitle = string.IsNullOrWhiteSpace(application.RoleTitle)
                ? string.IsNullOrWhiteSpace(application.PositionName)
                    ? $"Vacancy #{application.VacancyId}"
                    : application.PositionName.Trim()
                : application.RoleTitle.Trim(),
            LocationName = application.LocationName ?? string.Empty,
            EmploymentType = application.EmploymentType ?? string.Empty,
            JobFamilyName = application.JobFamilyName ?? string.Empty,
            SeniorityName = application.SeniorityName ?? string.Empty,
            JobDescription = application.JobDescription ?? string.Empty,
            MinSalary = application.MinSalary,
            MaxSalary = application.MaxSalary,
            Currency = application.Currency ?? string.Empty,
            HideSalary = application.HideSalary,
            ApplicationDeadline = application.ApplicationDeadline,
            VacancyStatus = application.VacancyStatus ?? string.Empty,
            ApplicationStatus = application.ApplicationStatus ?? string.Empty,
            FunnelStageName = string.IsNullOrWhiteSpace(application.FunnelStageName)
                ? "Applied"
                : application.FunnelStageName.Trim(),
            FunnelStageIndex = application.FunnelStageIndex,
            FunnelStageCount = application.FunnelStageCount,
            AppliedAtUtc = application.AppliedAtUtc,
            FunnelStageUpdatedAtUtc = application.FunnelStageUpdatedAtUtc,
            HiredAtUtc = application.HiredAtUtc,
            Skills = (application.Skills ?? [])
                .Where(skill => !string.IsNullOrWhiteSpace(skill.SkillName))
                .Select(skill => new CandidateApplicationSkillItem
                {
                    SkillId = skill.SkillId,
                    SkillName = skill.SkillName.Trim(),
                    Weight = Math.Max(skill.Weight, 0),
                    RequirementType = skill.RequirementType ?? string.Empty
                })
                .ToList()
        };
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
