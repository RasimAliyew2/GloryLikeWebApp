using System.Security.Claims;
using GloryLikeWebApp.Models.Student;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.StudentPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StudentProfileController(StudentProfileApiService students, IUserProfileDataApiService profiles) : Controller
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("/Student/Profile")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        var model = await BuildAsync(ct);
        model.StudentProfile.Saved = TempData["StudentProfileSaved"] is true;
        return View("~/Views/Student/Profile.cshtml", model);
    }

    [HttpPost("/Student/Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind(Prefix = "Input")] StudentProfileInput input, CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        if (ModelState.IsValid)
        {
            var result = await students.SaveProfileAsync(UserId, input, ct);
            if (result.Success)
            {
                await RefreshIdentityAsync(result.FirstName, result.LastName);
                TempData["StudentProfileSaved"] = true;
                return Redirect("/Student/Profile");
            }
            ModelState.AddModelError(string.Empty, result.Message);
        }

        var model = await BuildAsync(ct);
        model.StudentProfile.Input = input;
        // A failed read after submission must not discard the student's unsaved draft.
        if (!model.StudentProfile.IsAvailable)
        {
            model.Errors.Remove("Your profile could not be loaded. Please refresh before editing.");
            model.Errors.Add("Your saved profile is temporarily unavailable. Your changes are still below; try saving again.");
            model.StudentProfile.IsAvailable = true;
        }
        return View("~/Views/Student/Profile.cshtml", model);
    }

    private async Task<StudentDashboardViewModel> BuildAsync(CancellationToken ct)
    {
        var profileTask = students.GetAsync(UserId, ct);
        var skillsTask = profiles.GetAsync(UserId, ct);
        await Task.WhenAll(profileTask, skillsTask);
        var profile = await profileTask;
        var skills = await skillsTask;
        var model = new StudentDashboardViewModel
        {
            Page = "Profile", Education = profile,
            DisplayName = profile.Success && !string.IsNullOrWhiteSpace(profile.DisplayName)
                ? profile.DisplayName : string.Join(" ", new[] { User.FindFirstValue(ClaimTypes.Name), User.FindFirstValue(ClaimTypes.Surname) }.Where(value => !string.IsNullOrWhiteSpace(value))),
            SkillsAvailable = skills.Success && skills.Data is not null
        };
        if (string.IsNullOrWhiteSpace(model.DisplayName)) model.DisplayName = "Student";
        StudentDashboardBuilder.Populate(model, model.SkillsAvailable ? skills.Data!.Skills ?? [] : [], [], []);
        if (!model.SkillsAvailable) model.Errors.Add("Your verified skills and SSI could not be loaded. Please refresh to try again.");
        model.StudentProfile.IsAvailable = profile.Success;
        if (!profile.Success) model.Errors.Add("Your profile could not be loaded. Please refresh before editing.");
        else model.StudentProfile.Input = new StudentProfileInput
        {
            FirstName = profile.FirstName, LastName = profile.LastName,
            University = profile.University, Specialty = profile.Specialty, StudyYear = profile.StudyYear ?? 1,
            GraduationYear = profile.GraduationYear, About = profile.About, Goal = profile.Goal,
            OpenToInternships = profile.OpenToInternships, ProfileImageDataUrl = profile.ProfileImageDataUrl
        };
        return model;
    }

    private async Task RefreshIdentityAsync(string firstName, string lastName)
    {
        var authentication = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var claims = User.Claims.Where(claim => claim.Type != ClaimTypes.Name && claim.Type != ClaimTypes.Surname).ToList();
        claims.Add(new Claim(ClaimTypes.Name, firstName));
        claims.Add(new Claim(ClaimTypes.Surname, lastName));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            authentication.Properties ?? new AuthenticationProperties { IsPersistent = true });
    }
}
