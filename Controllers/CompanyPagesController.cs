using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GloryLikeWebApp.Controllers;

[AllowAnonymous]
public sealed class CompanyPagesController : Controller
{
    private readonly ICompanyProfileApiService _companyProfileApiService;

    public CompanyPagesController(ICompanyProfileApiService companyProfileApiService)
    {
        _companyProfileApiService = companyProfileApiService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; "
            + "script-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        base.OnActionExecuting(context);
    }

    [HttpGet("/companies/{companyOwnerUserId:int}/{slug?}")]
    public async Task<IActionResult> About(
        int companyOwnerUserId,
        string? slug,
        [FromQuery] bool preview,
        CancellationToken cancellationToken)
    {
        var model = await BuildModel(companyOwnerUserId, cancellationToken);
        model.IsPreview = preview;

        if (!string.IsNullOrWhiteSpace(model.ErrorMessage))
            Response.StatusCode = StatusCodes.Status404NotFound;

        return View("About", model);
    }

    [HttpGet("/companies/{companyOwnerUserId:int}/vacancies")]
    public async Task<IActionResult> Vacancies(
        int companyOwnerUserId,
        CancellationToken cancellationToken)
    {
        var model = await BuildModel(companyOwnerUserId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(model.ErrorMessage))
            Response.StatusCode = StatusCodes.Status404NotFound;
        return View("Vacancies", model);
    }

    [HttpGet("/companies/{companyOwnerUserId:int}/vacancies/{vacancyId:int}")]
    public async Task<IActionResult> Vacancy(
        int companyOwnerUserId,
        int vacancyId,
        CancellationToken cancellationToken)
    {
        var model = await BuildModel(companyOwnerUserId, cancellationToken);
        model.SelectedVacancy = model.Vacancies.FirstOrDefault(item => item.Id == vacancyId);

        if (model.SelectedVacancy is null)
        {
            model.ErrorMessage = "Vacancy tapılmadı və ya artıq aktiv deyil.";
            Response.StatusCode = StatusCodes.Status404NotFound;
        }

        return View("Vacancy", model);
    }

    [HttpGet("/companies/{companyOwnerUserId:int}/vacancies/{vacancyId:int}/apply")]
    public async Task<IActionResult> Apply(
        int companyOwnerUserId,
        int vacancyId,
        CancellationToken cancellationToken)
    {
        var model = await BuildModel(companyOwnerUserId, cancellationToken);
        model.SelectedVacancy = model.Vacancies.FirstOrDefault(item => item.Id == vacancyId);

        if (model.SelectedVacancy is null)
            return NotFound();

        var accountType = User.FindFirstValue("accountType");
        if (User.Identity?.IsAuthenticated == true
            && string.Equals(accountType, "candidate", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect($"/Opportunities/{vacancyId}/Apply");
        }

        ViewData["ReturnUrl"] = $"/Opportunities/{vacancyId}/Apply";
        ViewData["EmployerAccount"] = User.Identity?.IsAuthenticated == true;
        return View("ApplyGate", model);
    }

    private async Task<PublicCompanyPageViewModel> BuildModel(
        int companyOwnerUserId,
        CancellationToken cancellationToken)
    {
        var result = await _companyProfileApiService.GetPublicAsync(
            companyOwnerUserId,
            cancellationToken);

        if (!result.Success || result.Data?.Profile is null)
        {
            return new PublicCompanyPageViewModel
            {
                CompanyOwnerUserId = companyOwnerUserId,
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Company page tapılmadı."
                    : result.Message
            };
        }

        result.Data.Profile.Benefits ??= [];
        result.Data.Profile.Locations ??= [];

        return new PublicCompanyPageViewModel
        {
            CompanyOwnerUserId = result.Data.CompanyOwnerUserId,
            Profile = result.Data.Profile,
            Vacancies = result.Data.Vacancies ?? []
        };
    }
}
