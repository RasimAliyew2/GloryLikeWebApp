using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerVacanciesController : Controller
{
    private readonly ISkillAndJobApiService _skillAndJobApiService;
    private readonly ILogger<EmployerVacanciesController> _logger;

    public EmployerVacanciesController(
        ISkillAndJobApiService skillAndJobApiService,
        ILogger<EmployerVacanciesController> logger)
    {
        _skillAndJobApiService = skillAndJobApiService;
        _logger = logger;
    }

    [HttpGet("/Employer/Vacancies/Create")]
    public async Task<IActionResult> CreateVacancy(
        CancellationToken cancellationToken)
    {
        var model = await BuildPageModelAsync(
            new CreateVacancyInput
            {
                PlatformVacancyId = GenerateVacancyId(),
                ContactEmail =
                    User.FindFirstValue(ClaimTypes.Email)
                    ?? string.Empty,
                PublishDate = DateTime.Today
            },
            cancellationToken);

        return View("CreateVacancy", model);
    }

    [HttpPost("/Employer/Vacancies/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVacancy(
        CreateVacancyInput input,
        CancellationToken cancellationToken)
    {
        var model = await BuildPageModelAsync(
            input,
            cancellationToken);

        if (!model.HasTaxonomy)
            return View("CreateVacancy", model);

        ValidateSqlTaxonomy(
            input,
            model.JobFamilies);

        ValidateCompensation(input);
        ValidateFunnel(input);

        if (!ModelState.IsValid)
            return View("CreateVacancy", model);

        // BackendApp-də hazırda JobOffers üçün yalnız GET endpoint-i var.
        // Buna görə burada saxta SQL save əməliyyatı edilmir.
        // Form və bütün 5 mərhələ server-side validation-dan keçir.
        model.SuccessMessage =
            "Vacancy bütün 5 mərhələ üzrə yoxlanıldı. "
            + "Job, Seniority, Position və Skills SQL taxonomy-dən seçilib. "
            + "Backend-də vacancy POST endpoint-i əlavə ediləndən sonra "
            + "son Publish əməliyyatı SQL-ə yazıla bilər.";

        return View("CreateVacancy", model);
    }

    private async Task<CreateVacancyPageViewModel>
        BuildPageModelAsync(
            CreateVacancyInput input,
            CancellationToken cancellationToken)
    {
        var model = new CreateVacancyPageViewModel
        {
            DisplayName = GetDisplayName(),
            Email =
                User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty,
            Input = input
        };

        if (string.IsNullOrWhiteSpace(
            model.Input.PlatformVacancyId))
        {
            model.Input.PlatformVacancyId =
                GenerateVacancyId();
        }

        if (string.IsNullOrWhiteSpace(
            model.Input.ContactEmail))
        {
            model.Input.ContactEmail = model.Email;
        }

        var result =
            await _skillAndJobApiService
                .GetJobFamiliesAsync(
                    cancellationToken);

        if (!result.Success)
        {
            model.ErrorMessage =
                string.IsNullOrWhiteSpace(result.Message)
                    ? "SQL Job taxonomy yüklənmədi."
                    : result.Message;

            return model;
        }

        model.JobFamilies = result.JobFamilies
            .Where(
                job => job.Id > 0
                    && !string.IsNullOrWhiteSpace(
                        job.JobName))
            .OrderBy(job => job.JobName)
            .ToList();

        if (model.JobFamilies.Count == 0)
        {
            model.ErrorMessage =
                "SQL-də Job Family tapılmadı. "
                + "Vacancy formunda statik Job göstərilmir.";
        }

        return model;
    }

    private void ValidateSqlTaxonomy(
        CreateVacancyInput input,
        IReadOnlyCollection<JobFamily> jobFamilies)
    {
        var jobFamily = jobFamilies.FirstOrDefault(
            job => job.Id == input.JobFamilyId);

        if (jobFamily is null)
        {
            ModelState.AddModelError(
                nameof(input.JobFamilyId),
                "Seçilən Job SQL taxonomy-də yoxdur.");

            return;
        }

        var seniority =
            jobFamily.Seniorities.FirstOrDefault(
                item => item.Id == input.SeniorityId);

        if (seniority is null)
        {
            ModelState.AddModelError(
                nameof(input.SeniorityId),
                "Seçilən Seniority bu Job-a aid deyil.");

            return;
        }

        var position =
            seniority.Positions.FirstOrDefault(
                item => item.Id == input.PositionId);

        if (position is null)
        {
            ModelState.AddModelError(
                nameof(input.PositionId),
                "Seçilən Position bu Seniority-yə aid deyil.");

            return;
        }

        var allowedSkillIds = position.Skills
            .Select(skill => skill.Id)
            .Where(id => id > 0)
            .ToHashSet();

        var selectedSkillIds =
            input.SelectedSkillIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

        if (selectedSkillIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(input.SelectedSkillIds),
                "Ən azı bir SQL skill seçilməlidir.");
        }
        else if (selectedSkillIds.Any(
            id => !allowedSkillIds.Contains(id)))
        {
            ModelState.AddModelError(
                nameof(input.SelectedSkillIds),
                "Seçilən skill-lərdən biri Position-a aid deyil.");
        }

        input.RoleTitle = string.IsNullOrWhiteSpace(
            input.RoleTitle)
            ? position.Name
            : input.RoleTitle.Trim();
    }

    private void ValidateCompensation(
        CreateVacancyInput input)
    {
        if (input.MinSalary.HasValue
            && input.MaxSalary.HasValue
            && input.MinSalary.Value
                > input.MaxSalary.Value)
        {
            ModelState.AddModelError(
                nameof(input.MaxSalary),
                "Maximum salary Minimum salary-dən az ola bilməz.");
        }
    }

    private void ValidateFunnel(
        CreateVacancyInput input)
    {
        if (!input.StageApplied
            && !input.StageScreening
            && !input.StageInterview
            && !input.StageOffer)
        {
            ModelState.AddModelError(
                nameof(input.StageApplied),
                "Funnel üçün ən azı bir mərhələ seçilməlidir.");
        }
    }

    private string GetDisplayName()
    {
        var firstName =
            User.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;

        var surname =
            User.FindFirstValue(ClaimTypes.Surname)
            ?? string.Empty;

        var userName =
            User.FindFirstValue("username")
            ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(displayName)
            ? string.IsNullOrWhiteSpace(userName)
                ? "Employer"
                : userName
            : displayName;
    }

    private static string GenerateVacancyId()
    {
        return $"SM-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999)}";
    }
}
