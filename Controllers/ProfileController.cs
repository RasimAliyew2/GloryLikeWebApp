using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize]
public sealed class ProfileController : Controller
{
    private readonly IUserProfileDataApiService _userProfileDataApiService;
    private readonly IUserPersonalProfileApiService _personalProfileApiService;

    public ProfileController(
        IUserProfileDataApiService userProfileDataApiService,
        IUserPersonalProfileApiService personalProfileApiService)
    {
        _userProfileDataApiService = userProfileDataApiService;
        _personalProfileApiService = personalProfileApiService;
    }

    [HttpGet("/Profile")]
    public async Task<IActionResult> ProfilePage(
        [FromQuery] bool edit,
        [FromQuery] bool saved,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();

        if (userId is null)
            return Challenge();

        var model = await BuildModelAsync(
            userId.Value,
            edit,
            saved,
            cancellationToken);
        return View("ProfilePage", model);
    }

    [HttpPost("/Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(
        [Bind(Prefix = "Personal")] UserPersonalProfileInput input,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
            return Challenge();

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildModelAsync(
                userId.Value,
                true,
                false,
                cancellationToken);
            invalidModel.Personal = input;
            return View("ProfilePage", invalidModel);
        }

        var result = await _personalProfileApiService.UpdateAsync(
            userId.Value,
            input,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(
                string.Empty,
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Profile could not be saved."
                    : result.Message);

            var failedModel = await BuildModelAsync(
                userId.Value,
                true,
                false,
                cancellationToken);
            failedModel.Personal = input;
            return View("ProfilePage", failedModel);
        }

        await RefreshIdentityClaimsAsync(
            result.Data.FirstName,
            result.Data.LastName);

        return RedirectToAction(
            nameof(ProfilePage),
            new { saved = true });
    }

    private async Task<ProfilePageViewModel> BuildModelAsync(
        int userId,
        bool edit,
        bool saved,
        CancellationToken cancellationToken)
    {
        var accountType = User.FindFirstValue("accountType") ?? "candidate";
        var model = new ProfilePageViewModel
        {
            UserId = userId,
            AccountType = accountType,
            DisplayName = GetDisplayName(accountType),
            UserName = User.FindFirstValue("username") ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            PhoneNumber = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty,
            IsEditMode = edit,
            SuccessMessage = saved ? "Profile saved successfully." : null,
            Personal = new UserPersonalProfileInput
            {
                FirstName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                LastName = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty
            }
        };

        var personalResult = await _personalProfileApiService.GetAsync(
            userId,
            cancellationToken);

        if (personalResult.Success && personalResult.Data is not null)
        {
            var personal = personalResult.Data;
            model.Personal = new UserPersonalProfileInput
            {
                FirstName = personal.FirstName,
                LastName = personal.LastName,
                BirthDate = personal.BirthDate,
                About = personal.About,
                ProfileImageDataUrl = personal.ProfileImageDataUrl
            };
            model.Email = personal.Email;
            model.AccountType = string.IsNullOrWhiteSpace(personal.AccountType)
                ? model.AccountType
                : personal.AccountType;
            model.DisplayName = JoinDisplayName(
                personal.FirstName,
                personal.LastName,
                model.UserName,
                model.IsEmployer ? "Employer" : "Candidate");
        }
        else
        {
            model.ErrorMessage = personalResult.Message;
        }

        if (!model.IsEmployer)
        {
            var profileResult = await _userProfileDataApiService.GetAsync(
                userId,
                cancellationToken);

            if (profileResult.Success && profileResult.Data is not null)
            {
                model.Skills = profileResult.Data.Skills?
                    .OrderByDescending(skill => skill.CalculatedCredibilityScore)
                    .ThenBy(skill => skill.SkillName)
                    .ToList()
                    ?? new List<UserSkillInfo>();

                model.WorkExperiences = profileResult.Data.Experiences?
                    .OrderByDescending(experience => ParseYear(experience.EndYear))
                    .ThenByDescending(experience => ParseYear(experience.StartYear))
                    .ToList()
                    ?? new List<UserWorkExperienceInfo>();

                model.CurrentJobName = ResolveCurrentJobName(model.Skills);
            }
            else
            {
                model.ErrorMessage = CombineErrors(
                    model.ErrorMessage,
                    profileResult.Message);
            }
        }

        return model;
    }

    private async Task RefreshIdentityClaimsAsync(
        string firstName,
        string lastName)
    {
        var authentication = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        var claims = User.Claims
            .Where(claim => claim.Type != ClaimTypes.Name
                && claim.Type != ClaimTypes.Surname)
            .ToList();
        claims.Add(new Claim(ClaimTypes.Name, firstName));
        claims.Add(new Claim(ClaimTypes.Surname, lastName));

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authentication.Properties ?? new AuthenticationProperties
            {
                IsPersistent = true
            });
    }

    private int? GetRequiredUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId) && userId > 0
            ? userId
            : null;
    }

    private string GetDisplayName(string accountType)
    {
        var firstName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var surname = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var userName = User.FindFirstValue("username") ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return string.IsNullOrWhiteSpace(userName)
            ? string.Equals(
                accountType,
                "employer",
                StringComparison.OrdinalIgnoreCase)
                ? "Employer"
                : "Candidate"
            : userName;
    }

    private static string JoinDisplayName(
        string? firstName,
        string? lastName,
        string? userName,
        string fallback)
    {
        var fullName = string.Join(
            " ",
            new[] { firstName, lastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return string.IsNullOrWhiteSpace(userName)
            ? fallback
            : userName;
    }

    private static string CombineErrors(string? first, string? second)
    {
        return string.Join(
            " ",
            new[] { first, second }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string ResolveCurrentJobName(
        IReadOnlyCollection<UserSkillInfo> skills)
    {
        return skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill.JobFamilyName))
            .GroupBy(
                skill => skill.JobFamilyName.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault()
            ?? string.Empty;
    }

    private static int ParseYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DateTime.UtcNow.Year;

        if (value.Trim().Equals(
                "Present",
                StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Year;
        }

        return int.TryParse(value.Trim(), out var year)
            ? year
            : 0;
    }
}
