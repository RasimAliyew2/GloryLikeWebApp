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
        NormalizeSkillRequirements(input);

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

        model.SuccessMessage =
            "Vacancy bütün 5 mərhələ üzrə yoxlanıldı. "
            + "Job, Seniority, Position və Skill-lər SQL taxonomy-dən seçilib. "
            + "Hər skill üçün Required/Desirable statusu və 1–100 verification "
            + "level saxlanılıb. Backend vacancy POST endpoint-i əlavə ediləndən "
            + "sonra bu məlumatlar SQL-ə yazıla bilər.";

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
                "Input.JobFamilyId",
                "Seçilən Job SQL taxonomy-də yoxdur.");

            return;
        }

        var seniority =
            jobFamily.Seniorities.FirstOrDefault(
                item => item.Id == input.SeniorityId);

        if (seniority is null)
        {
            ModelState.AddModelError(
                "Input.SeniorityId",
                "Seçilən Seniority bu Job-a aid deyil.");

            return;
        }

        var position =
            seniority.Positions.FirstOrDefault(
                item => item.Id == input.PositionId);

        if (position is null)
        {
            ModelState.AddModelError(
                "Input.PositionId",
                "Seçilən Position bu Seniority-yə aid deyil.");

            return;
        }

        ValidateSkillRequirements(
            input,
            jobFamilies);

        input.RoleTitle = string.IsNullOrWhiteSpace(
            input.RoleTitle)
            ? position.Name
            : input.RoleTitle.Trim();
    }

    private void ValidateSkillRequirements(
        CreateVacancyInput input,
        IReadOnlyCollection<JobFamily> jobFamilies)
    {
        NormalizeSkillRequirements(input);

        var allSqlSkillIds = jobFamilies
            .SelectMany(job => job.Seniorities)
            .SelectMany(seniority => seniority.Positions)
            .SelectMany(position => position.Skills)
            .Where(skill => skill.Id > 0)
            .Select(skill => skill.Id)
            .ToHashSet();

        if (input.SkillRequirements.Count == 0)
        {
            ModelState.AddModelError(
                "Input.SelectedSkillIds",
                "Ən azı bir SQL skill seçilməlidir.");

            return;
        }

        var duplicateSkillIds = input.SkillRequirements
            .GroupBy(requirement => requirement.SkillId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateSkillIds.Count > 0)
        {
            ModelState.AddModelError(
                "Input.SelectedSkillIds",
                "Eyni skill bir dəfədən çox əlavə edilə bilməz.");
        }

        foreach (var requirement in input.SkillRequirements)
        {
            if (!allSqlSkillIds.Contains(requirement.SkillId))
            {
                ModelState.AddModelError(
                    "Input.SelectedSkillIds",
                    $"SkillId {requirement.SkillId} SQL taxonomy-də tapılmadı.");
            }

            if (requirement.MinimumVerificationLevel is < 1 or > 100)
            {
                ModelState.AddModelError(
                    "Input.SelectedSkillIds",
                    "Hər skill üçün verification level 1–100 arasında olmalıdır.");
            }

            if (!requirement.RequirementType.Equals(
                    "Required",
                    StringComparison.OrdinalIgnoreCase)
                && !requirement.RequirementType.Equals(
                    "Desirable",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "Input.SelectedSkillIds",
                    "Skill statusu Required və ya Desirable olmalıdır.");
            }
        }
    }

    private static void NormalizeSkillRequirements(
        CreateVacancyInput input)
    {
        input.SkillRequirements ??=
            new List<VacancySkillRequirementInput>();

        input.SelectedSkillIds ??= new List<int>();

        // Köhnə form yalnız SelectedSkillIds göndərərsə,
        // onları yeni modelə çevir.
        if (input.SkillRequirements.Count == 0
            && input.SelectedSkillIds.Count > 0)
        {
            input.SkillRequirements = input.SelectedSkillIds
                .Where(id => id > 0)
                .Distinct()
                .Select(
                    id => new VacancySkillRequirementInput
                    {
                        SkillId = id,
                        MinimumVerificationLevel =
                            Math.Clamp(
                                input.MinimumVerificationLevel,
                                1,
                                100),
                        RequirementType = "Required"
                    })
                .ToList();
        }

        input.SkillRequirements = input.SkillRequirements
            .Where(requirement => requirement.SkillId > 0)
            .Select(
                requirement => new VacancySkillRequirementInput
                {
                    SkillId = requirement.SkillId,
                    MinimumVerificationLevel = Math.Clamp(
                        requirement.MinimumVerificationLevel,
                        1,
                        100),
                    RequirementType =
                        requirement.RequirementType.Equals(
                            "Desirable",
                            StringComparison.OrdinalIgnoreCase)
                            ? "Desirable"
                            : "Required"
                })
            .ToList();

        input.SelectedSkillIds = input.SkillRequirements
            .Select(requirement => requirement.SkillId)
            .Distinct()
            .ToList();

        if (input.SkillRequirements.Count > 0)
        {
            input.MinimumVerificationLevel =
                input.SkillRequirements[0]
                    .MinimumVerificationLevel;
        }
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
                "Input.MaxSalary",
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
                "Input.StageApplied",
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
