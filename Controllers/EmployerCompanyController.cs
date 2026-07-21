using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerCompanyController : Controller
{
    [HttpGet("/Employer/Company")]
    public IActionResult Index()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = int.TryParse(userIdValue, out var userId);

        return View("CompanyProfile", new CompanyProfilePageViewModel
        {
            UserId = userId,
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        });
    }

    private string GetDisplayName()
    {
        var firstName = User.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        var surname = User.FindFirstValue(ClaimTypes.Surname)
            ?? string.Empty;
        var userName = User.FindFirstValue("username")
            ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(displayName)
            ? string.IsNullOrWhiteSpace(userName)
                ? "Employer"
                : userName
            : displayName;
    }
}
