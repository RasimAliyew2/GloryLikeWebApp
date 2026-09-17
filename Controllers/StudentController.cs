using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Student;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.StudentPolicy)]
public sealed class StudentController(StudentProfileApiService students, IUserProfileDataApiService profiles,
    IVacancyApiService vacancies) : Controller
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("/Student", Name = "StudentDashboard")]
    [HttpGet("/Student/Home")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        return View(await BuildAsync(ct));
    }

    [HttpGet("/Student/Internships")]
    public async Task<IActionResult> Internships(CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        var model = await BuildAsync(ct);
        model.Page = "Internships";
        return View(model);
    }

    [HttpPost("/Student/Education")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEducation(StudentEducationInput input, CancellationToken ct)
    {
        if (UserId <= 0) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Enter your university, specialty and study year (1–5)." });
        var result = await students.SaveAsync(UserId, input, ct);
        return StatusCode(result.Success ? 200 : 400, new { success = result.Success, message = result.Message });
    }

    private async Task<StudentDashboardViewModel> BuildAsync(CancellationToken ct)
    {
        var educationTask = students.GetAsync(UserId, ct);
        var skillsTask = profiles.GetAsync(UserId, ct);
        var opportunitiesTask = vacancies.GetCandidateVacanciesAsync(UserId, ct);
        var applicationsTask = vacancies.GetCandidateApplicationsAsync(UserId, ct);
        await Task.WhenAll(educationTask, skillsTask, opportunitiesTask, applicationsTask);
        var education = await educationTask;
        var skills = await skillsTask;
        var opportunities = await opportunitiesTask;
        var applications = await applicationsTask;
        var model = new StudentDashboardViewModel
        {
            Education = education,
            DisplayName = string.IsNullOrWhiteSpace(education.DisplayName)
                ? User.FindFirstValue(ClaimTypes.Name) ?? "Student" : education.DisplayName,
            SkillsAvailable = skills.Success && skills.Data is not null,
            OpportunitiesAvailable = opportunities.Success && opportunities.Data is not null,
            ApplicationsAvailable = applications.Success && applications.Data is not null
        };
        if (!education.Success) model.Errors.Add("Education details are temporarily unavailable.");
        if (!model.SkillsAvailable) model.Errors.Add("Your skills and SSI could not be loaded. Please refresh to try again.");
        if (!model.OpportunitiesAvailable) model.Errors.Add("Opportunities could not be loaded. Please try again later.");
        if (!model.ApplicationsAvailable) model.Errors.Add("Your internship applications could not be loaded.");
        StudentDashboardBuilder.Populate(model, skills.Success ? skills.Data?.Skills ?? [] : [],
            opportunities.Success ? opportunities.Data?.Vacancies ?? [] : [],
            applications.Success ? applications.Data?.Applications ?? [] : []);
        return model;
    }
}
