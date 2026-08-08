using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerReportsController : Controller
{
    private readonly IOrganizationReportsApiService _reportsApiService;

    public EmployerReportsController(
        IOrganizationReportsApiService reportsApiService)
    {
        _reportsApiService = reportsApiService;
    }

    [HttpGet("/Employer/Reports")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model = new OrganizationReportsPageViewModel
        {
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };

        if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var actorUserId)
            || actorUserId <= 0)
        {
            model.ErrorMessage = "Employer user ID tapılmadı.";
            return View("Reports", model);
        }

        var result = await _reportsApiService.GetAsync(
            actorUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Organization reports yüklənmədi."
                : result.Message;
            return View("Reports", model);
        }

        model.CompanyName = result.Data.CompanyName;
        model.GeneratedAtUtc = result.Data.GeneratedAtUtc;
        model.Categories = result.Data.Categories;

        return View("Reports", model);
    }

    private string GetDisplayName()
    {
        var name = string.Join(
            " ",
            new[]
            {
                User.FindFirstValue(ClaimTypes.Name),
                User.FindFirstValue(ClaimTypes.Surname)
            }.Where(item => !string.IsNullOrWhiteSpace(item)));

        return string.IsNullOrWhiteSpace(name)
            ? User.FindFirstValue("username") ?? "Employer"
            : name;
    }
}
