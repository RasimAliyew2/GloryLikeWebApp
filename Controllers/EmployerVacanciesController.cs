using System.Diagnostics;
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
    private const int MaximumScreeningQuestionCount = 20;
    private const int MaximumFunnelStageCount = 20;

    private readonly ISkillAndJobApiService _skillAndJobApiService;
    private readonly IVacancyApiService _vacancyApiService;
    private readonly ILogger<EmployerVacanciesController> _logger;

    public EmployerVacanciesController(
        ISkillAndJobApiService skillAndJobApiService,
        IVacancyApiService vacancyApiService,
        ILogger<EmployerVacanciesController> logger)
    {
        _skillAndJobApiService = skillAndJobApiService;
        _vacancyApiService = vacancyApiService;
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
                PublishDate = DateTime.Today,
                ScreeningQuestions = new List<VacancyScreeningQuestionInput>
                {
                    new()
                },
                FunnelStages = CreateDefaultFunnelStages()
            },
            cancellationToken);

        model.SuccessMessage =
            TempData["VacancySuccessMessage"]
            as string;

        return View("CreateVacancy", model);
    }

    [HttpPost("/Employer/Vacancies/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVacancy(
        CreateVacancyInput input,
        CancellationToken cancellationToken)
    {
        NormalizeSkillRequirements(input);
        NormalizeBenefits(input);
        NormalizeApplicationRequirements(input);
        NormalizeScreeningQuestions(input);
        NormalizeFunnelStages(input);
        NormalizePublication(input);

        input.ClientRequisitionCode =
        input.ClientRequisitionCode?.Trim() ?? string.Empty;

        input.JobDescription =
            input.JobDescription?.Trim() ?? string.Empty;

        input.ScreeningNotes =
            input.ScreeningNotes?.Trim() ?? string.Empty;

        var model = await BuildPageModelAsync(
            input,
            cancellationToken);

        if (!model.HasTaxonomy)
            return View("CreateVacancy", model);

        ValidateSqlTaxonomy(
            input,
            model.JobFamilies);

        ValidateCompensation(input);
        ValidateScreeningQuestions(input);
        ValidateFunnel(input);
        ValidatePublication(input);

        if (!ModelState.IsValid)
            return View("CreateVacancy", model);

        if (!TryGetEmployerUserId(out var employerUserId))
        {
            model.SubmissionErrorMessage =
                "Login məlumatında employer user ID tapılmadı. Yenidən sign in edin.";
            model.OpenPublicationStageOnLoad = true;

            return View("CreateVacancy", model);
        }

        var createResult = await _vacancyApiService.CreateAsync(
            employerUserId,
            input,
            cancellationToken);

        if (!createResult.Success)
        {
            _logger.LogWarning(
                "Vacancy {PlatformVacancyId} BackendApp-də yaradılmadı: {Message}",
                input.PlatformVacancyId,
                createResult.Message);

            model.SubmissionErrorMessage =
                string.IsNullOrWhiteSpace(createResult.Message)
                    ? "Vacancy BackendApp-də yaradılmadı."
                    : createResult.Message;
            model.OpenPublicationStageOnLoad = true;

            return View("CreateVacancy", model);
        }

        _logger.LogInformation(
            "Vacancy {VacancyId}/{PlatformVacancyId} BackendApp vasitəsilə SQL-də yaradıldı.",
            createResult.VacancyId,
            createResult.PlatformVacancyId);

        TempData["VacancySuccessMessage"] =
            $"Vacancy SQL-də yaradıldı. ID: {createResult.VacancyId}, "
            + $"Platform ID: {createResult.PlatformVacancyId}.";

        return RedirectToAction(nameof(CreateVacancy));
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


    private static void NormalizeApplicationRequirements(
        CreateVacancyInput input)
    {
        input.ApplicationRequirements ??=
            new ApplicationRequirementsInput();

        input.ApplicationRequirements.CustomFields ??=
            new List<ApplicationCustomFieldInput>();

        input.ApplicationRequirements.CustomFields =
            input.ApplicationRequirements.CustomFields
                .Where(field =>
                    !string.IsNullOrWhiteSpace(field.Label))
                .Select(field => new ApplicationCustomFieldInput
                {
                    Label = field.Label.Trim(),
                    Requirement = Enum.IsDefined(field.Requirement)
                        ? field.Requirement
                        : ApplicationRequirementMode.Optional
                })
                .GroupBy(
                    field => field.Label,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(20)
                .ToList();
    }

    private static void NormalizeBenefits(
        CreateVacancyInput input)
    {
        input.Benefits ??= new List<string>();

        input.Benefits = input.Benefits
            .Where(benefit => !string.IsNullOrWhiteSpace(benefit))
            .Select(benefit => benefit.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void NormalizeScreeningQuestions(
        CreateVacancyInput input)
    {
        input.ScreeningQuestions ??=
            new List<VacancyScreeningQuestionInput>();

        foreach (var question in input.ScreeningQuestions)
        {
            question.QuestionText =
                question.QuestionText?.Trim()
                ?? string.Empty;

            question.AnswerType =
                question.AnswerType?.Trim()
                ?? string.Empty;

            question.RequirementType =
                question.RequirementType?.Trim()
                ?? string.Empty;
        }
    }

    private void ValidateScreeningQuestions(
        CreateVacancyInput input)
    {
        if (input.ScreeningQuestions.Count
            <= MaximumScreeningQuestionCount)
        {
            return;
        }

        ModelState.AddModelError(
            "Input.ScreeningQuestions",
            $"Maksimum {MaximumScreeningQuestionCount} screening sualı əlavə edilə bilər.");
    }

    private static void NormalizeFunnelStages(
        CreateVacancyInput input)
    {
        input.FunnelStages ??=
            new List<VacancyFunnelStageInput>();

        foreach (var stage in input.FunnelStages)
        {
            stage.StageName =
                stage.StageName?.Trim()
                ?? string.Empty;
        }

        input.StageApplied = input.FunnelStages.Any(
            stage =>
                stage.StageName.Equals(
                    "Applied",
                    StringComparison.OrdinalIgnoreCase)
                || stage.StageName.Equals(
                    "Applications",
                    StringComparison.OrdinalIgnoreCase)
                || stage.StageName.Equals(
                    "Responses",
                    StringComparison.OrdinalIgnoreCase));

        input.StageScreening = input.FunnelStages.Any(
            stage => stage.StageName.Contains(
                "Screening",
                StringComparison.OrdinalIgnoreCase));

        input.StageInterview = input.FunnelStages.Any(
            stage => stage.StageName.Contains(
                "Interview",
                StringComparison.OrdinalIgnoreCase));

        input.StageOffer = input.FunnelStages.Any(
            stage => stage.StageName.Contains(
                "Offer",
                StringComparison.OrdinalIgnoreCase));
    }

    private static void NormalizePublication(
        CreateVacancyInput input)
    {
        input.Visibility =
            input.Visibility?.Trim()
            ?? string.Empty;

        input.ContactEmail =
            input.ContactEmail?.Trim()
            ?? string.Empty;

        // SkillMatch əsas publication kanalıdır və dizayna əsasən
        // bütün vacancy-lər üçün həmişə aktiv qalır.
        input.PublishOnSkillMatch = true;
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
        if (input.FunnelStages.Count == 0)
        {
            ModelState.AddModelError(
                "Input.FunnelStages",
                "Funnel üçün ən azı bir mərhələ seçilməlidir.");
        }

        if (input.FunnelStages.Count > MaximumFunnelStageCount)
        {
            ModelState.AddModelError(
                "Input.FunnelStages",
                $"Maksimum {MaximumFunnelStageCount} funnel mərhələsi əlavə edilə bilər.");
        }

        var duplicateStage = input.FunnelStages
            .Where(stage =>
                !string.IsNullOrWhiteSpace(stage.StageName))
            .GroupBy(
                stage => stage.StageName,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateStage is not null)
        {
            ModelState.AddModelError(
                "Input.FunnelStages",
                $"“{duplicateStage.Key}” mərhələsi yalnız bir dəfə əlavə edilə bilər.");
        }
    }

    private void ValidatePublication(
        CreateVacancyInput input)
    {
        var allowedVisibilityValues = new[]
        {
            "Public",
            "Internal",
            "Anonymous"
        };

        if (!allowedVisibilityValues.Contains(
            input.Visibility,
            StringComparer.Ordinal))
        {
            ModelState.AddModelError(
                "Input.Visibility",
                "Publication type Public, Internal və ya Anonymous olmalıdır.");
        }

        if (input.PublishDate.HasValue
            && input.ApplicationDeadline.HasValue
            && input.ApplicationDeadline.Value.Date
                < input.PublishDate.Value.Date)
        {
            ModelState.AddModelError(
                "Input.ApplicationDeadline",
                "Application deadline publish date-dən əvvəl ola bilməz.");
        }
    }

    private static List<VacancyFunnelStageInput>
        CreateDefaultFunnelStages()
    {
        return new List<VacancyFunnelStageInput>
        {
            new()
            {
                StageName = "Responses",
                Hours = 48,
                IsStandard = true
            },
            new()
            {
                StageName = "Screening",
                Hours = 72,
                IsStandard = true
            },
            new()
            {
                StageName = "Interview",
                Hours = 120,
                IsStandard = true
            },
            new()
            {
                StageName = "Offer",
                Hours = 48,
                IsStandard = true
            },
            new()
            {
                StageName = "Hired",
                Hours = 0,
                IsStandard = true
            }
        };
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

    private bool TryGetEmployerUserId(out int userId)
    {
        return int.TryParse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier),
                out userId)
            && userId > 0;
    }

    private static string GenerateVacancyId()
    {
        return $"SM-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999)}";
    }
}
