using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployeePolicy)]
public sealed class ProfileController : Controller
{
    private readonly IUserProfileDataApiService _userProfileDataApiService;

    public ProfileController(IUserProfileDataApiService userProfileDataApiService)
    {
        _userProfileDataApiService = userProfileDataApiService;
    }

    [HttpGet("/Profile")]
    public async Task<IActionResult> ProfilePage(
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();

        if (userId is null)
            return Challenge();

        var model = new ProfilePageViewModel
        {
            UserId = userId.Value,
            DisplayName = GetDisplayName(),
            UserName = User.FindFirstValue("username") ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            PhoneNumber = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty
        };

        var profileResult = await _userProfileDataApiService.GetAsync(
            userId.Value,
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
            model.ErrorMessage = profileResult.Message;
        }

        return View("ProfilePage", model);
    }

    private int? GetRequiredUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId) && userId > 0
            ? userId
            : null;
    }

    private string GetDisplayName()
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
            ? "Candidate"
            : userName;
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
