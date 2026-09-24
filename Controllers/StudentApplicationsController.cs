using System.Security.Claims;
using GloryLikeWebApp.Models.Student;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.StudentPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StudentApplicationsController(IVacancyApiService vacancies, IUserProfileDataApiService profiles) : Controller
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("/Student/Applications")]
    public async Task<IActionResult> Index(string? search, string? status, int? vacancyId, CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        var model = await BuildAsync(ct);
        var history = model.ApplicationHistory;
        history.SearchText = search?.Trim() ?? "";
        var normalizedStatus = status?.Trim().ToLowerInvariant();
        history.StatusFilter = normalizedStatus is "in-progress" or "hired" or "not-selected" or "withdrawn"
            ? normalizedStatus : "all";
        history.HighlightVacancyId = vacancyId;
        if (history.StatusFilter != "all")
            history.Applications = history.Applications.Where(application => application.StatusKey == history.StatusFilter).ToList();
        if (history.SearchText.Length > 0)
            history.Applications = history.Applications.Where(application => application.SearchText.Contains(history.SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        return View(model);
    }

    [HttpGet("/Student/Applications/{vacancyId:int}")]
    public async Task<IActionResult> Details(int vacancyId, CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        var model = await BuildAsync(ct);
        var history = model.ApplicationHistory;
        if (history.ErrorMessage.Length == 0)
        {
            history.SelectedApplication = history.Applications.FirstOrDefault(application => application.Details.VacancyId == vacancyId);
            if (history.SelectedApplication is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                history.ErrorMessage = "This application was not found in your application history.";
            }
        }
        return View(model);
    }

    private async Task<StudentDashboardViewModel> BuildAsync(CancellationToken ct)
    {
        var applicationsTask = vacancies.GetCandidateApplicationsAsync(UserId, ct);
        var skillsTask = profiles.GetAsync(UserId, ct);
        await Task.WhenAll(applicationsTask, skillsTask);
        var response = await applicationsTask;
        var skills = await skillsTask;
        var model = new StudentDashboardViewModel
        {
            Page = "Applications", DisplayName = User.FindFirstValue(ClaimTypes.Name) ?? "Student",
            SkillsAvailable = skills.Success && skills.Data is not null,
            ApplicationsAvailable = response.Success && response.Data is not null
        };
        StudentDashboardBuilder.Populate(model, skills.Success ? skills.Data?.Skills ?? [] : [], [], []);
        var history = model.ApplicationHistory;
        if (!model.ApplicationsAvailable)
        {
            history.ErrorMessage = "Your applications are temporarily unavailable. Please try again.";
            return model;
        }
        // Application history belongs to the student even after a vacancy closes or changes category.
        history.Applications = response.Data!.Applications.OrderByDescending(application => application.AppliedAtUtc)
            .ThenByDescending(application => application.ApplicationId)
            .Select(application => new StudentApplicationViewItem
            {
                Details = ApplicationViewMapper.Map(application), VacancyType = application.VacancyType
            }).ToList();
        history.TotalCount = history.Applications.Count;
        history.InProgressCount = history.Applications.Count(application => application.StatusKey == "in-progress");
        history.HiredCount = history.Applications.Count(application => application.StatusKey == "hired");
        history.NotSelectedCount = history.Applications.Count(application => application.StatusKey == "not-selected");
        history.WithdrawnCount = history.Applications.Count(application => application.StatusKey == "withdrawn");
        return model;
    }
}
