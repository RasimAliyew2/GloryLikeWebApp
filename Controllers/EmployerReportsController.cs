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
        [FromQuery] OrganizationReportsQuery query,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var defaultFrom = today.AddMonths(-11);
        defaultFrom = new DateTime(
            defaultFrom.Year,
            defaultFrom.Month,
            1);

        var model = new OrganizationReportsPageViewModel
        {
            DateFrom = query.DateFrom?.Date ?? defaultFrom,
            DateTo = query.DateTo?.Date ?? today,
            ActiveTab = OrganizationReportsPageViewModel.NormalizeTab(query.Tab)
        };
        PopulateShell(model);

        if (!TryGetActorUserId(out var actorUserId))
            return Challenge();

        var result = await _reportsApiService.GetDashboardAsync(
            actorUserId,
            model.DateFrom,
            model.DateTo,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = ResolveError(
                result.Message,
                "Analytics dashboard could not be loaded.");
            return View("Reports", model);
        }

        result.Data.MonthlyActivity ??= [];
        result.Data.FunnelStages ??= [];
        result.Data.Sources ??= [];
        result.Data.TeamMembers ??= [];
        result.Data.VacancyTimings ??= [];

        model.CompanyName = result.Data.CompanyName;
        model.DateFrom = result.Data.DateFrom.Date;
        model.DateTo = result.Data.DateTo.Date;
        model.Dashboard = result.Data;

        return View("Reports", model);
    }

    [HttpGet("/Employer/Reports/VacancyCreation")]
    public IActionResult LegacyVacancyCreation(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        return RedirectToAction(
            nameof(Index),
            new
            {
                DateFrom = dateFrom?.ToString("yyyy-MM-dd"),
                DateTo = dateTo?.ToString("yyyy-MM-dd")
            });
    }

    private bool TryGetActorUserId(out int actorUserId) =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out actorUserId)
        && actorUserId > 0;

    private void PopulateShell(EmployerReportsShellViewModel model)
    {
        model.DisplayName = GetDisplayName();
        model.Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
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

    private static string ResolveError(string message, string fallback)
    {
        return string.IsNullOrWhiteSpace(message) ? fallback : message;
    }
}
