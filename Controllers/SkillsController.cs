using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployeePolicy)]
public sealed class SkillsController : Controller
{
    private readonly IUserProfileDataApiService _userProfileDataApiService;
    private readonly ISkillAndJobApiService _skillAndJobApiService;
    private readonly ISkillAssessmentApiService _skillAssessmentApiService;

    public SkillsController(
        IUserProfileDataApiService userProfileDataApiService,
        ISkillAndJobApiService skillAndJobApiService,
        ISkillAssessmentApiService skillAssessmentApiService)
    {
        _userProfileDataApiService = userProfileDataApiService;
        _skillAndJobApiService = skillAndJobApiService;
        _skillAssessmentApiService = skillAssessmentApiService;
    }

    [HttpGet("/Skills")]
    public async Task<IActionResult> SkillsPage(
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();

        if (userId is null)
            return Challenge();

        var model = await BuildPageModelAsync(
            userId.Value,
            cancellationToken);

        model.SuccessMessage = TempData["SkillsSuccess"] as string;

        if (TempData["SkillsError"] is string error)
            model.ErrorMessage = error;

        if (TempData["AssessmentSkillId"] is int skillId)
            model.AutoAssessmentSkillId = skillId;

        model.AutoAssessmentSkillName =
            TempData["AssessmentSkillName"] as string
            ?? string.Empty;

        return View("SkillsPage", model);
    }

    [HttpPost("/Skills/AddJob")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddJob(
        [Bind(Prefix = "AddJob")] AddJobRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();

        if (userId is null)
            return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["SkillsError"] = "Job seçilməlidir.";
            return RedirectToAction(nameof(SkillsPage));
        }

        var profileResult = await _userProfileDataApiService.GetAsync(
            userId.Value,
            cancellationToken);

        if (!profileResult.Success || profileResult.Data is null)
        {
            TempData["SkillsError"] = profileResult.Message;
            return RedirectToAction(nameof(SkillsPage));
        }

        var taxonomyResult = await _skillAndJobApiService
            .GetJobFamiliesAsync(cancellationToken);

        if (!taxonomyResult.Success)
        {
            TempData["SkillsError"] = taxonomyResult.Message;
            return RedirectToAction(nameof(SkillsPage));
        }

        var selectedJob = taxonomyResult.JobFamilies.FirstOrDefault(
            job => job.Id == request.JobFamilyId);

        if (selectedJob is null)
        {
            TempData["SkillsError"] =
                "Seçilən Job SQL taxonomy-də tapılmadı.";
            return RedirectToAction(nameof(SkillsPage));
        }

        var saveResult = await _userProfileDataApiService.SaveAsync(
            userId.Value,
            new UserJobInfo
            {
                JobFamilyId = selectedJob.Id,
                JobFamilyName = selectedJob.JobName
            },
            profileResult.Data.Skills ?? new List<UserSkillInfo>(),
            profileResult.Data.Experiences
                ?? new List<UserWorkExperienceInfo>(),
            cancellationToken);

        TempData[saveResult.Success
            ? "SkillsSuccess"
            : "SkillsError"] = saveResult.Success
            ? $"{selectedJob.JobName} Job kimi saxlandı. İndi istənilən skill-i əlavə edə bilərsiniz."
            : saveResult.Message;

        return RedirectToAction(nameof(SkillsPage));
    }

    [HttpPost("/Skills/AddSkill")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSkill(
        [Bind(Prefix = "AddSkill")] AddSkillRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();

        if (userId is null)
            return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["SkillsError"] = ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .FirstOrDefault()
                ?? "Skill seçilməyib.";

            return RedirectToAction(nameof(SkillsPage));
        }

        var profileResult = await _userProfileDataApiService.GetAsync(
            userId.Value,
            cancellationToken);

        if (!profileResult.Success || profileResult.Data is null)
        {
            TempData["SkillsError"] = profileResult.Message;
            return RedirectToAction(nameof(SkillsPage));
        }

        var skills = profileResult.Data.Skills
            ?? new List<UserSkillInfo>();

        var experiences = profileResult.Data.Experiences
            ?? new List<UserWorkExperienceInfo>();

        var currentJob = profileResult.Data.Job;
        if (currentJob is null || currentJob.JobFamilyId <= 0)
        {
            TempData["SkillsError"] =
                "Skill əlavə etməzdən əvvəl Job seçilməlidir.";

            return RedirectToAction(nameof(SkillsPage));
        }

        var skillLookupResult =
            await _skillAndJobApiService.GetAllSkillsAsync(
                cancellationToken);

        if (!skillLookupResult.Success)
        {
            TempData["SkillsError"] = skillLookupResult.Message;
            return RedirectToAction(nameof(SkillsPage));
        }

        var availableSkills = BuildAvailableSkills(
            skillLookupResult.Skills,
            currentJob.JobFamilyId,
            skills);

        var selectedSkill = availableSkills.FirstOrDefault(
            x => string.Equals(
                x.SelectionKey,
                request.SelectionKey,
                StringComparison.Ordinal));

        if (selectedSkill is null)
        {
            TempData["SkillsError"] =
                "Seçilən skill tapılmadı və ya artıq əlavə olunub.";

            return RedirectToAction(nameof(SkillsPage));
        }

        skills.Add(new UserSkillInfo
        {
            SkillId = selectedSkill.SkillId,
            SkillName = selectedSkill.SkillName,
            PositionId = selectedSkill.PositionId,
            PositionName = selectedSkill.PositionName,
            SeniorityId = selectedSkill.SeniorityId,
            SeniorityName = selectedSkill.SeniorityName,
            JobFamilyId = selectedSkill.JobFamilyId,
            JobFamilyName = selectedSkill.JobFamilyName,
            SkillComplexity = selectedSkill.SkillComplexity,
            Status = "self_declared",
            IsVerified = false,
            KnowledgeScore = 0,
            ExperienceScore = 0,
            DepthScore = 0,
            CredibilityScore = 0
        });

        var saveResult = await _userProfileDataApiService.SaveAsync(
            userId.Value,
            currentJob,
            skills,
            experiences,
            cancellationToken);

        TempData[saveResult.Success
            ? "SkillsSuccess"
            : "SkillsError"] = saveResult.Success
            ? $"{selectedSkill.SkillName} əlavə olundu. AI skill testi açılır."
            : saveResult.Message;

        if (saveResult.Success)
        {
            TempData["AssessmentSkillId"] = selectedSkill.SkillId;
            TempData["AssessmentSkillName"] = selectedSkill.SkillName;
        }

        return RedirectToAction(nameof(SkillsPage));
    }

    [HttpPost("/Skills/AddExperience")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExperience(
        [Bind(Prefix = "AddExperience")] AddExperienceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();

        if (userId is null)
            return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["SkillsError"] = ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .FirstOrDefault()
                ?? "Experience formu düzgün doldurulmayıb.";

            return RedirectToAction(nameof(SkillsPage));
        }

        var profileResult = await _userProfileDataApiService.GetAsync(
            userId.Value,
            cancellationToken);

        if (!profileResult.Success || profileResult.Data is null)
        {
            TempData["SkillsError"] = profileResult.Message;
            return RedirectToAction(nameof(SkillsPage));
        }

        var skills = profileResult.Data.Skills
            ?? new List<UserSkillInfo>();

        var experiences = profileResult.Data.Experiences
            ?? new List<UserWorkExperienceInfo>();

        var normalizedEndYear = string.IsNullOrWhiteSpace(
            request.EndYear)
            ? "Present"
            : request.EndYear.Trim();

        var duplicate = experiences.Any(x =>
            x.CompanyName.Equals(
                request.CompanyName.Trim(),
                StringComparison.OrdinalIgnoreCase)
            && x.PositionName.Equals(
                request.PositionName.Trim(),
                StringComparison.OrdinalIgnoreCase)
            && x.StartYear.Equals(
                request.StartYear.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            TempData["SkillsError"] =
                "Bu experience artıq profile əlavə olunub.";

            return RedirectToAction(nameof(SkillsPage));
        }

        experiences.Add(new UserWorkExperienceInfo
        {
            CompanyName = request.CompanyName.Trim(),
            PositionName = request.PositionName.Trim(),
            StartYear = request.StartYear.Trim(),
            EndYear = normalizedEndYear,
            FileName = request.FileName?.Trim() ?? string.Empty
        });

        var saveResult = await _userProfileDataApiService.SaveAsync(
            userId.Value,
            profileResult.Data.Job,
            skills,
            experiences,
            cancellationToken);

        TempData[saveResult.Success
            ? "SkillsSuccess"
            : "SkillsError"] = saveResult.Success
            ? "Experience əlavə olundu və SQL-də saxlandı."
            : saveResult.Message;

        return RedirectToAction(nameof(SkillsPage));
    }

    [HttpPost("/Skills/Assessment/Generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAssessment(
        [FromBody] StartSkillAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();

        if (userId is null)
            return Unauthorized(new { success = false, message = "Sessiya tapılmadı." });

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Skill və test dili düzgün seçilməyib."
            });
        }

        var profileResult = await _userProfileDataApiService.GetAsync(
            userId.Value,
            cancellationToken);

        if (!profileResult.Success || profileResult.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = profileResult.Message
            });
        }

        var skill = FindUserSkill(
            profileResult.Data.Skills,
            request.SkillId,
            request.SkillName);

        if (skill is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Skill candidate profilində tapılmadı."
            });
        }

        var apiResult = await _skillAssessmentApiService.GenerateAsync(
            new GenerateSkillQuestionnaireRequest
            {
                Skill = skill.SkillName,
                SkillComplexity = NormalizeAssessmentComplexity(
                    skill.SkillComplexity),
                Seniority = NormalizeAssessmentSeniority(
                    skill.SeniorityName),
                Language = request.Language.Trim().ToLowerInvariant()
            },
            cancellationToken);

        if (!apiResult.Success || apiResult.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = apiResult.Message
            });
        }

        return Json(new
        {
            success = true,
            questionnaire = apiResult.Data
        });
    }

    [HttpPost("/Skills/Assessment/Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAssessment(
        [FromBody] CompleteSkillAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();

        if (userId is null)
            return Unauthorized(new { success = false, message = "Sessiya tapılmadı." });

        if (!ModelState.IsValid || request.QuestionnaireId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = "Bütün görünən suallar cavablandırılmalıdır."
            });
        }

        var profileResult = await _userProfileDataApiService.GetAsync(
            userId.Value,
            cancellationToken);

        if (!profileResult.Success || profileResult.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = profileResult.Message
            });
        }

        var skill = FindUserSkill(
            profileResult.Data.Skills,
            request.SkillId,
            request.SkillName);

        if (skill is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Skill candidate profilində tapılmadı."
            });
        }

        var assessmentResult = await _skillAssessmentApiService.SubmitAsync(
            new SubmitSkillDepthAssessmentRequest
            {
                QuestionnaireId = request.QuestionnaireId,
                Answers = request.Answers
            },
            cancellationToken);

        if (!assessmentResult.Success || assessmentResult.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = assessmentResult.Message
            });
        }

        var result = assessmentResult.Data;
        if (!result.Skill.Equals(
                skill.SkillName,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                success = false,
                message = "Questionnaire seçilən skill-ə aid deyil."
            });
        }

        var complexityScore = Math.Round(
            Math.Clamp(result.ComplexityRatio, 0d, 1d) * 100d,
            2);
        var ownershipScore = Math.Round(
            Math.Clamp(result.OwnershipRatio, 0d, 1d) * 100d,
            2);
        var depthDimensionScore = Math.Round(
            Math.Clamp(result.DepthRatio, 0d, 1d) * 100d,
            2);

        // Mobile App-dəki MVP scoring mapping-i ilə eyni saxlanılır.
        skill.KnowledgeScore = result.DepthScore;
        skill.ExperienceScore = complexityScore;
        skill.DepthScore = result.DepthScore;
        skill.CredibilityScore = Math.Clamp(
            (skill.KnowledgeScore * 0.45d)
            + (skill.ExperienceScore * 0.55d),
            0d,
            100d);
        skill.ContextScore = result.DepthScore;
        skill.ComplexityScore = complexityScore;
        skill.OwnershipScore = ownershipScore;
        skill.ResultScore = depthDimensionScore;
        skill.TaskComplexity = result.TaskComplexity;
        skill.OwnershipLevel = result.OwnershipLevel;
        skill.DepthTier = result.DepthTier;
        skill.Status = "verified";
        skill.IsVerified = true;

        var saveResult = await _userProfileDataApiService.SaveAsync(
            userId.Value,
            profileResult.Data.Job,
            profileResult.Data.Skills ?? new List<UserSkillInfo>(),
            profileResult.Data.Experiences
                ?? new List<UserWorkExperienceInfo>(),
            cancellationToken);

        if (!saveResult.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = saveResult.Message
            });
        }

        return Json(new
        {
            success = true,
            message = $"{skill.SkillName} üzrə nəticə SQL-də saxlandı.",
            score = result.DepthScore,
            credibilityScore = (int)Math.Floor(
                skill.CredibilityScore + 0.5d),
            result.DepthTier,
            result.TaskComplexity,
            result.OwnershipLevel
        });
    }

    private async Task<SkillsPageViewModel> BuildPageModelAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var model = new SkillsPageViewModel
        {
            UserId = userId,
            DisplayName = GetDisplayName(),
            UserName = User.FindFirstValue("username")
                ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty
        };

        var profileResult = await _userProfileDataApiService.GetAsync(
            userId,
            cancellationToken);

        if (profileResult.Success && profileResult.Data is not null)
        {
            model.Skills = profileResult.Data.Skills?
                .OrderByDescending(
                    x => x.CalculatedCredibilityScore)
                .ThenBy(x => x.SkillName)
                .ToList()
                ?? new List<UserSkillInfo>();

            model.WorkExperiences =
                profileResult.Data.Experiences?
                    .OrderByDescending(
                        x => ParseYear(x.EndYear))
                    .ThenByDescending(
                        x => ParseYear(x.StartYear))
                    .ToList()
                ?? new List<UserWorkExperienceInfo>();

            if (profileResult.Data.Job is not null)
            {
                model.CurrentJobFamilyId =
                    profileResult.Data.Job.JobFamilyId;
                model.CurrentJobName =
                    profileResult.Data.Job.JobFamilyName;
                model.AddJob.JobFamilyId =
                    profileResult.Data.Job.JobFamilyId;
            }
        }
        else
        {
            model.ErrorMessage = profileResult.Message;
        }

        var taxonomyResult =
            await _skillAndJobApiService.GetJobFamiliesAsync(
                cancellationToken);

        if (!taxonomyResult.Success)
        {
            model.JobFilterMessage = taxonomyResult.Message;
            return model;
        }

        model.AvailableJobs = taxonomyResult.JobFamilies
            .OrderBy(job => job.JobName)
            .ToList();

        if (!model.HasCurrentJob)
        {
            model.JobFilterMessage =
                "Əvvəlcə Job əlavə edin. Bundan sonra bütün sistem skill-ləri aktiv olacaq.";

            return model;
        }

        var skillLookupResult =
            await _skillAndJobApiService.GetAllSkillsAsync(
                cancellationToken);

        if (!skillLookupResult.Success)
        {
            model.JobFilterMessage = skillLookupResult.Message;
            return model;
        }

        model.AvailableSkills = BuildAvailableSkills(
            skillLookupResult.Skills,
            model.CurrentJobFamilyId,
            model.Skills);

        if (model.AvailableSkills.Count == 0)
        {
            model.JobFilterMessage =
                "Sistemdəki bütün skill-lər artıq profile əlavə olunub.";
        }

        return model;
    }

    private static List<AvailableSkillItem> BuildAvailableSkills(
        IReadOnlyCollection<SkillLookupItem> allSkills,
        int currentJobFamilyId,
        IReadOnlyCollection<UserSkillInfo> existingSkills)
    {
        var existingSkillIds = existingSkills
            .Where(x => x.SkillId > 0)
            .Select(x => x.SkillId)
            .ToHashSet();

        var existingSkillNames = existingSkills
            .Where(x => !string.IsNullOrWhiteSpace(
                x.SkillName))
            .Select(x => x.SkillName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allSkills
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.SkillName)
                && !existingSkillIds.Contains(x.Id)
                && !existingSkillNames.Contains(
                    x.SkillName.Trim()))
            .GroupBy(
                x => x.SkillName.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(skill =>
                    skill.JobFamilyId == currentJobFamilyId)
                .ThenBy(skill => skill.JobFamilyName)
                .ThenBy(skill => skill.PositionName)
                .ThenBy(skill => skill.Id)
                .First())
            .Select(skill =>
            {
                var seniority = skill.Seniorities
                    .OrderByDescending(option =>
                        option.Name.Equals(
                            "Middle",
                            StringComparison.OrdinalIgnoreCase))
                    .ThenBy(option => option.SortOrder)
                    .FirstOrDefault();

                return new AvailableSkillItem
                {
                    SkillId = skill.Id,
                    SkillName = skill.SkillName.Trim(),
                    PositionId = skill.PositionId,
                    PositionName = skill.PositionName,
                    SeniorityId = seniority?.Id ?? 0,
                    SeniorityName = seniority?.Name ?? "Middle",
                    JobFamilyId = skill.JobFamilyId,
                    JobFamilyName = skill.JobFamilyName,
                    SkillComplexity = string.IsNullOrWhiteSpace(
                        skill.SkillComplexity)
                        ? "medium"
                        : skill.SkillComplexity
                            .Trim()
                            .ToLowerInvariant()
                };
            })
            .OrderBy(x => x.SkillName)
            .ToList();
    }

    private static UserSkillInfo? FindUserSkill(
        IReadOnlyCollection<UserSkillInfo>? skills,
        int skillId,
        string? skillName)
    {
        if (skills is null)
            return null;

        return skills.FirstOrDefault(skill =>
            (skillId > 0 && skill.SkillId == skillId)
            || (!string.IsNullOrWhiteSpace(skillName)
                && skill.SkillName.Equals(
                    skillName.Trim(),
                    StringComparison.OrdinalIgnoreCase)));
    }

    private static string NormalizeAssessmentComplexity(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "low" => "low",
            "high" => "high",
            _ => "medium"
        };
    }

    private static string NormalizeAssessmentSeniority(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "junior" => "junior",
            "senior" => "senior",
            "lead" => "lead",
            "head" => "lead",
            _ => "middle"
        };
    }

    private int? GetRequiredUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
               && userId > 0
            ? userId
            : null;
    }

    private string GetDisplayName()
    {
        var firstName = User.FindFirstValue(
            ClaimTypes.Name)
            ?? string.Empty;

        var surname = User.FindFirstValue(
            ClaimTypes.Surname)
            ?? string.Empty;

        var userName = User.FindFirstValue("username")
            ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        return string.IsNullOrWhiteSpace(displayName)
            ? string.IsNullOrWhiteSpace(userName)
                ? "Candidate"
                : userName
            : displayName;
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

        return int.TryParse(
            value.Trim(),
            out var year)
            ? year
            : 0;
    }
}
