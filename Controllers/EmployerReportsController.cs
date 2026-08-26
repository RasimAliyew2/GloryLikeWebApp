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
    private static readonly HashSet<string> AllowedFields =
    [
        VacancyCreationReportPageViewModel.EmployeeEmailField,
        VacancyCreationReportPageViewModel.EmployeeRoleField,
        VacancyCreationReportPageViewModel.VacancyCountField,
        VacancyCreationReportPageViewModel.EmployeeDatesField,
        VacancyCreationReportPageViewModel.VacanciesField,
        VacancyCreationReportPageViewModel.VacancyDateField,
        VacancyCreationReportPageViewModel.VacancyStatusField
    ];

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
        var model = new OrganizationReportsPageViewModel();
        PopulateShell(model);

        if (!TryGetActorUserId(out var actorUserId))
            return Challenge();

        var result = await _reportsApiService.GetCatalogAsync(
            actorUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = ResolveError(
                result.Message,
                "Report catalog could not be loaded.");
            return View("Reports", model);
        }

        result.Data.Reports ??= [];
        model.CompanyName = result.Data.CompanyName;
        model.Reports = result.Data.Reports;

        return View("Reports", model);
    }

    [HttpGet("/Employer/Reports/VacancyCreation")]
    public async Task<IActionResult> VacancyCreation(
        [FromQuery] VacancyCreationReportQuery query,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var model = new VacancyCreationReportPageViewModel
        {
            DateFrom = query.DateFrom?.Date
                ?? new DateTime(today.Year, today.Month, 1),
            DateTo = query.DateTo?.Date ?? today,
            WasExecuted = query.Execute,
            SelectedFields = query.Execute
                ? query.Fields
                    .Where(AllowedFields.Contains)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : VacancyCreationReportPageViewModel.DefaultFields()
        };
        PopulateShell(model);

        if (!TryGetActorUserId(out var actorUserId))
            return Challenge();

        if (!query.Execute)
        {
            var catalogResult = await _reportsApiService.GetCatalogAsync(
                actorUserId,
                cancellationToken);

            if (catalogResult.Success && catalogResult.Data is not null)
                model.CompanyName = catalogResult.Data.CompanyName;
            else
                model.ErrorMessage = ResolveError(
                    catalogResult.Message,
                    "Report settings could not be loaded.");

            return View("VacancyCreation", model);
        }

        var result = await _reportsApiService.ExecuteVacancyCreationReportAsync(
            actorUserId,
            model.DateFrom,
            model.DateTo,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = ResolveError(
                result.Message,
                "Report could not be generated.");
            return View("VacancyCreation", model);
        }

        result.Data.Employees ??= [];
        foreach (var employee in result.Data.Employees)
        {
            employee.Vacancies ??= [];
            employee.VacancyCreationDatesUtc ??= [];
        }

        model.CompanyName = result.Data.CompanyName;
        model.ReportTitle = result.Data.ReportTitle;
        model.GeneratedAtUtc = result.Data.GeneratedAtUtc;
        model.TotalVacancyCount = result.Data.TotalVacancyCount;
        model.Employees = result.Data.Employees;

        return View("VacancyCreation", model);
    }

    [HttpGet("/Employer/Reports/Employees/{employeeUserId:int}")]
    public async Task<IActionResult> EmployeeProfile(
        int employeeUserId,
        CancellationToken cancellationToken)
    {
        var model = new ReportEmployeeProfilePageViewModel();
        PopulateShell(model);

        if (!TryGetActorUserId(out var actorUserId))
            return Challenge();

        var result = await _reportsApiService.GetEmployeeProfileAsync(
            actorUserId,
            employeeUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = ResolveError(
                result.Message,
                "Employee profile could not be loaded.");
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("EmployeeProfile", model);
        }

        model.CompanyName = result.Data.CompanyName;
        model.Employee = result.Data;
        return View("EmployeeProfile", model);
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
