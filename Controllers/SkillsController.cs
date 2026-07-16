using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize]
public sealed class SkillsController : Controller
{
    private readonly IUserProfileDataApiService _userProfileDataApiService;

    public SkillsController(IUserProfileDataApiService userProfileDataApiService)
    {
        _userProfileDataApiService = userProfileDataApiService;
    }

    [HttpGet("/Skills")]
    public async Task<IActionResult> SkillsPage(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId) || userId <= 0)
            return Challenge();

        var firstName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var surname = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var userName = User.FindFirstValue("username") ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = string.IsNullOrWhiteSpace(userName)
                ? "Candidate"
                : userName;

        var apiResult = await _userProfileDataApiService.GetAsync(
            userId,
            cancellationToken);

        var model = new SkillsPageViewModel
        {
            UserId = userId,
            DisplayName = displayName,
            UserName = userName,
            Email = email
        };

        if (apiResult.Success && apiResult.Data is not null)
        {
            model.Skills = apiResult.Data.Skills?
                .OrderByDescending(x => x.CalculatedCredibilityScore)
                .ThenBy(x => x.SkillName)
                .ToList()
                ?? new List<UserSkillInfo>();

            model.WorkExperiences = apiResult.Data.Experiences?
                .OrderByDescending(x => ParseYear(x.EndYear))
                .ThenByDescending(x => ParseYear(x.StartYear))
                .ToList()
                ?? new List<UserWorkExperienceInfo>();
        }
        else
        {
            model.ErrorMessage = apiResult.Message;
        }

        return View("SkillsPage", model);
    }

    private static int ParseYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DateTime.UtcNow.Year;

        if (value.Trim().Equals("Present", StringComparison.OrdinalIgnoreCase))
            return DateTime.UtcNow.Year;

        return int.TryParse(value.Trim(), out var year)
            ? year
            : 0;
    }
}
