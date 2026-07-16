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

    public SkillsController(
        IUserProfileDataApiService userProfileDataApiService,
        ISkillAndJobApiService skillAndJobApiService)
    {
        _userProfileDataApiService = userProfileDataApiService;
        _skillAndJobApiService = skillAndJobApiService;
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

        return View("SkillsPage", model);
    }

    [HttpPost("/Skills/AddSkill")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSkill(
        AddSkillRequest request,
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

        var taxonomyResult =
            await _skillAndJobApiService.GetJobFamiliesAsync(
                cancellationToken);

        if (!taxonomyResult.Success)
        {
            TempData["SkillsError"] = taxonomyResult.Message;
            return RedirectToAction(nameof(SkillsPage));
        }

        var selectedJob = ResolveCurrentJob(
            taxonomyResult.JobFamilies,
            skills);

        if (selectedJob is null)
        {
            TempData["SkillsError"] =
                "İstifadəçinin Job məlumatı tapılmadı. Skill-lər Job-a görə filtr olunmadan göstərilə bilməz.";

            return RedirectToAction(nameof(SkillsPage));
        }

        var availableSkills = BuildAvailableSkills(
            selectedJob,
            skills);

        var selectedSkill = availableSkills.FirstOrDefault(
            x => string.Equals(
                x.SelectionKey,
                request.SelectionKey,
                StringComparison.Ordinal));

        if (selectedSkill is null)
        {
            TempData["SkillsError"] =
                "Seçilən skill user-in Job-u üçün uyğun deyil və ya artıq əlavə olunub.";

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
            skills,
            experiences,
            cancellationToken);

        TempData[saveResult.Success
            ? "SkillsSuccess"
            : "SkillsError"] = saveResult.Success
            ? $"{selectedSkill.SkillName} əlavə olundu və SQL-də saxlandı."
            : saveResult.Message;

        return RedirectToAction(nameof(SkillsPage));
    }

    [HttpPost("/Skills/AddExperience")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExperience(
        AddExperienceRequest request,
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

        var currentJob = ResolveCurrentJob(
            taxonomyResult.JobFamilies,
            model.Skills);

        if (currentJob is null)
        {
            model.JobFilterMessage =
                "User-in Job məlumatı SQL-də tapılmadığı üçün əlavə edilə bilən skill siyahısı göstərilmir.";

            return model;
        }

        model.CurrentJobName = currentJob.JobName;
        model.AvailableSkills = BuildAvailableSkills(
            currentJob,
            model.Skills);

        if (model.AvailableSkills.Count == 0)
        {
            model.JobFilterMessage =
                "Bu Job üçün bütün uyğun skill-lər artıq profile əlavə olunub.";
        }

        return model;
    }

    private JobFamily? ResolveCurrentJob(
        IReadOnlyCollection<JobFamily> jobFamilies,
        IReadOnlyCollection<UserSkillInfo> existingSkills)
    {
        var claimJobFamilyId = User.FindFirstValue(
            "jobFamilyId");

        if (int.TryParse(
                claimJobFamilyId,
                out var jobFamilyId)
            && jobFamilyId > 0)
        {
            var byClaimId = jobFamilies.FirstOrDefault(
                x => x.Id == jobFamilyId);

            if (byClaimId is not null)
                return byClaimId;
        }

        var claimJobFamilyName = User.FindFirstValue(
            "jobFamilyName");

        if (!string.IsNullOrWhiteSpace(claimJobFamilyName))
        {
            var byClaimName = jobFamilies.FirstOrDefault(
                x => x.JobName.Equals(
                    claimJobFamilyName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (byClaimName is not null)
                return byClaimName;
        }

        var mostFrequentJobFamilyId = existingSkills
            .Where(x => x.JobFamilyId > 0)
            .GroupBy(x => x.JobFamilyId)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefault();

        if (mostFrequentJobFamilyId > 0)
        {
            var bySavedId = jobFamilies.FirstOrDefault(
                x => x.Id == mostFrequentJobFamilyId);

            if (bySavedId is not null)
                return bySavedId;
        }

        var mostFrequentJobFamilyName = existingSkills
            .Where(x => !string.IsNullOrWhiteSpace(
                x.JobFamilyName))
            .GroupBy(
                x => x.JobFamilyName.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(
                mostFrequentJobFamilyName))
        {
            return jobFamilies.FirstOrDefault(
                x => x.JobName.Equals(
                    mostFrequentJobFamilyName,
                    StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static List<AvailableSkillItem> BuildAvailableSkills(
        JobFamily selectedJob,
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

        return selectedJob.Seniorities
            .SelectMany(
                seniority => seniority.Positions.Select(
                    position => new
                    {
                        Seniority = seniority,
                        Position = position
                    }))
            .SelectMany(
                x => x.Position.Skills.Select(
                    skill => new AvailableSkillItem
                    {
                        SkillId = skill.Id,
                        SkillName = skill.SkillName,
                        PositionId = x.Position.Id,
                        PositionName = x.Position.Name,
                        SeniorityId = x.Seniority.Id,
                        SeniorityName = x.Seniority.Name,
                        JobFamilyId = selectedJob.Id,
                        JobFamilyName = selectedJob.JobName,
                        SkillComplexity =
                            string.IsNullOrWhiteSpace(
                                skill.SkillComplexity)
                                ? "medium"
                                : skill.SkillComplexity
                                    .Trim()
                                    .ToLowerInvariant()
                    }))
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.SkillName)
                && !existingSkillIds.Contains(x.SkillId)
                && !existingSkillNames.Contains(
                    x.SkillName.Trim()))
            .GroupBy(
                x => x.SkillName.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.SkillName)
            .ToList();
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
