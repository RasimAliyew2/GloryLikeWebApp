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

        if (AccountRouting.Normalize(User.FindFirstValue("accountType")) == "student")
            return RedirectToAction("Index", "StudentApplications", new { vacancyId });

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
            .Select(ApplicationViewMapper.Map)
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

        if (AccountRouting.Normalize(User.FindFirstValue("accountType")) == "student")
            return RedirectToAction("Details", "StudentApplications", new { vacancyId });

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

        model.Application = ApplicationViewMapper.Map(application);
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
