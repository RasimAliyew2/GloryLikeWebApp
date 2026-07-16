using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize]
public sealed class OpportunitiesController : Controller
{
    private readonly ILogger<OpportunitiesController> _logger;
    private readonly IUserProfileDataApiService _userProfileDataApiService;
    private readonly IJobOffersApiService _jobOffersApiService;

    public OpportunitiesController(
        ILogger<OpportunitiesController> logger,
        IUserProfileDataApiService userProfileDataApiService,
        IJobOffersApiService jobOffersApiService)
    {
        _logger = logger;
        _userProfileDataApiService = userProfileDataApiService;
        _jobOffersApiService = jobOffersApiService;
    }

    [HttpGet("/Opportunities")]
    public async Task<IActionResult> OpportunitiesPage(
        string? search,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId)
            || userId <= 0)
        {
            return Challenge();
        }

        var model = CreateBaseModel(userId, search);

        var profileResult =
            await _userProfileDataApiService.GetAsync(
                userId,
                cancellationToken);

        if (!profileResult.Success
            || profileResult.Data is null)
        {
            model.ErrorMessage =
                string.IsNullOrWhiteSpace(profileResult.Message)
                    ? "SQL profile məlumatı yüklənmədi."
                    : profileResult.Message;

            return View("OpportunitiesPage", model);
        }

        var selectedSkills =
            profileResult.Data.Skills
            ?? new List<UserSkillInfo>();

        model.CurrentJobName =
            ResolveCurrentJobName(selectedSkills);

        // Strict Job filter:
        // Job müəyyən edilməyibsə bütün elanları göstərmirik.
        if (string.IsNullOrWhiteSpace(model.CurrentJobName))
        {
            model.EmptyMessage =
                "User-in cari Job məlumatı SQL profile datasında tapılmadı. "
                + "Job müəyyən edilmədən elanlar filtrsiz göstərilmir.";

            return View("OpportunitiesPage", model);
        }

        try
        {
            var jobOffers =
                await _jobOffersApiService.GetJobOffersAsync(
                    cancellationToken);

            var opportunities = BuildOpportunities(
                jobOffers,
                model.CurrentJobName,
                selectedSkills);

            if (!string.IsNullOrWhiteSpace(model.SearchText))
            {
                opportunities = opportunities
                    .Where(
                        opportunity => opportunity.SearchText
                            .Contains(
                                model.SearchText,
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            model.Opportunities = opportunities;

            if (model.Opportunities.Count == 0)
            {
                model.EmptyMessage =
                    string.IsNullOrWhiteSpace(model.SearchText)
                        ? $"SQL-də “{model.CurrentJobName}” Job-u üçün uyğun JobOffer tapılmadı."
                        : $"“{model.SearchText}” axtarışına uyğun {model.CurrentJobName} JobOffer-i tapılmadı.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Opportunities could not be loaded for user {UserId}.",
                userId);

            model.ErrorMessage =
                "JobOffers SQL datası yüklənmədi: "
                + ex.Message;
        }

        return View("OpportunitiesPage", model);
    }

    private OpportunitiesPageViewModel CreateBaseModel(
        int userId,
        string? search)
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
                    value => !string.IsNullOrWhiteSpace(value)));

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = string.IsNullOrWhiteSpace(userName)
                ? "Candidate"
                : userName;
        }

        return new OpportunitiesPageViewModel
        {
            UserId = userId,
            DisplayName = displayName,
            UserName = userName,
            Email = User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty,
            SearchText = search?.Trim() ?? string.Empty
        };
    }

    private string ResolveCurrentJobName(
        IReadOnlyCollection<UserSkillInfo> selectedSkills)
    {
        // Login/backend gələcəkdə Job claim göndərərsə,
        // həmin dəyər SQL skill fallback-dan əvvəl istifadə ediləcək.
        var jobClaim =
            User.FindFirstValue("jobFamilyName");

        if (!string.IsNullOrWhiteSpace(jobClaim))
            return jobClaim.Trim();

        // Hazırkı BackendApp profile contract-da Job ayrıca
        // qaytarılmadığı üçün cari Job SQL-də saxlanan
        // skill-lərin JobFamilyName dəyərindən müəyyən edilir.
        return selectedSkills
            .Where(
                skill => !string.IsNullOrWhiteSpace(
                    skill.JobFamilyName))
            .GroupBy(
                skill => skill.JobFamilyName.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault()
            ?? string.Empty;
    }

    private static List<OpportunityItem> BuildOpportunities(
        IReadOnlyList<JobOfferApiItem> jobOffers,
        string currentJobName,
        IReadOnlyCollection<UserSkillInfo> selectedSkills)
    {
        var candidateSkills =
            BuildCandidateSkillScoreMap(selectedSkills);

        // Vacib: Job filter score hesablanmazdan əvvəl tətbiq edilir.
        // HR user-in siyahısına programmer offer heç vaxt daxil olmur.
        var matchingJobOffers = jobOffers
            .Where(offer => offer is not null)
            .Where(
                offer => !string.IsNullOrWhiteSpace(
                    offer.RequiredJob))
            .Where(
                offer => !string.IsNullOrWhiteSpace(
                    offer.Skills))
            .Where(offer => offer.SkillsWeight > 0)
            .Where(
                offer => Normalize(offer.RequiredJob)
                    == Normalize(currentJobName))
            .ToList();

        var groups = matchingJobOffers
            .GroupBy(
                offer => new
                {
                    RequiredJob = offer.RequiredJob.Trim(),
                    Name = string.IsNullOrWhiteSpace(offer.Name)
                        ? BuildTitle(
                            offer.RequiredJob,
                            offer.Seniority)
                        : offer.Name.Trim(),
                    Description =
                        offer.Description?.Trim()
                        ?? string.Empty,
                    Seniority =
                        string.IsNullOrWhiteSpace(
                            offer.Seniority)
                            ? "Middle"
                            : offer.Seniority.Trim()
                })
            .ToList();

        var result = new List<OpportunityItem>();
        var index = 0;

        foreach (var group in groups)
        {
            var requiredSkills = group
                .Select(
                    offer => new RequiredSkillTemplate
                    {
                        SkillName = offer.Skills.Trim(),
                        Weight = Math.Max(
                            offer.SkillsWeight,
                            1)
                    })
                .Where(
                    skill => !string.IsNullOrWhiteSpace(
                        skill.SkillName))
                .GroupBy(
                    skill => Normalize(skill.SkillName),
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    skillGroup => new RequiredSkillTemplate
                    {
                        SkillName =
                            skillGroup.First().SkillName,
                        Weight =
                            skillGroup.Max(
                                skill => skill.Weight)
                    })
                .ToList();

            if (requiredSkills.Count == 0)
                continue;

            var score = CalculateRoleReadiness(
                requiredSkills,
                candidateSkills);

            var matchedSkills = requiredSkills
                .Where(
                    skill => candidateSkills.ContainsKey(
                        Normalize(skill.SkillName)))
                .Select(skill => skill.SkillName)
                .OrderBy(skill => skill)
                .ToList();

            var missingSkills = requiredSkills
                .Where(
                    skill => !candidateSkills.ContainsKey(
                        Normalize(skill.SkillName)))
                .Select(skill => skill.SkillName)
                .OrderBy(skill => skill)
                .ToList();

            var responsibilities = requiredSkills
                .OrderByDescending(skill => skill.Weight)
                .ThenBy(skill => skill.SkillName)
                .Take(5)
                .Select(
                    skill =>
                        $"{skill.SkillName} — weight {skill.Weight}")
                .ToList();

            result.Add(
                new OpportunityItem
                {
                    Id = group.Min(offer => offer.Id),

                    LogoLetter =
                        GetLogoLetter(group.Key.RequiredJob),

                    // Mobile App da Company sahəsinə RequiredJob yazır.
                    // Bu dəyər SQL JobOffers-dan gəlir.
                    Company = group.Key.RequiredJob,

                    Title = group.Key.Name,
                    Level = group.Key.Seniority,

                    // Backend modelində location/work type/salary yoxdur.
                    // Saxta data yaratmamaq üçün yalnız SQL-dən
                    // hesablana bilən məlumat göstərilir.
                    Location = "SQL JobOffer",
                    WorkType = "Role",
                    Salary =
                        requiredSkills.Count == 1
                            ? "1 required skill"
                            : $"{requiredSkills.Count} required skills",

                    Score = score,
                    ScoreColor = GetScoreColor(score),
                    IsExpanded = index == 0,

                    AboutRole =
                        string.IsNullOrWhiteSpace(
                            group.Key.Description)
                            ? "Bu JobOffer üçün description SQL-də qeyd edilməyib."
                            : group.Key.Description,

                    Responsibilities =
                        string.Join(
                            Environment.NewLine,
                            responsibilities.Select(
                                item => $"• {item}")),

                    MatchedSkills =
                        matchedSkills.Count == 0
                            ? "No matched skills yet"
                            : string.Join(", ", matchedSkills),

                    MissingSkills =
                        missingSkills.Count == 0
                            ? "No missing required skills"
                            : string.Join(
                                ", ",
                                missingSkills.Take(8)),

                    MatchNote = BuildMatchNote(
                        score,
                        matchedSkills.Count,
                        requiredSkills.Count),

                    RequiredSkillsCount =
                        requiredSkills.Count,

                    RequiredSkillItems =
                        requiredSkills
                            .Select(skill => skill.SkillName)
                            .ToList(),

                    MatchedSkillItems = matchedSkills,
                    MissingSkillItems = missingSkills,

                    ResponsibilityItems =
                        responsibilities
                });

            index++;
        }

        return result
            .OrderByDescending(opportunity => opportunity.Score)
            .ThenBy(opportunity => opportunity.Title)
            .ToList();
    }

    /// <summary>
    /// Mobile App OpportunitiesViewModel ilə eyni score mənbə seçimi:
    /// DepthScore varsa onu, yoxdursa KnowledgeScore-u istifadə edir.
    /// </summary>
    private static Dictionary<string, double>
        BuildCandidateSkillScoreMap(
            IReadOnlyCollection<UserSkillInfo> selectedSkills)
    {
        return selectedSkills
            .Where(
                skill => !string.IsNullOrWhiteSpace(
                    skill.SkillName))
            .GroupBy(
                skill => Normalize(skill.SkillName),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Max(GetCandidateSkillScore),
                StringComparer.OrdinalIgnoreCase);
    }

    private static double GetCandidateSkillScore(
        UserSkillInfo skill)
    {
        if (skill.DepthScore > 0)
        {
            return Math.Clamp(
                skill.DepthScore,
                0,
                100);
        }

        if (skill.KnowledgeScore > 0)
        {
            return Math.Clamp(
                skill.KnowledgeScore,
                0,
                100);
        }

        return 0;
    }

    private static int CalculateRoleReadiness(
        IReadOnlyCollection<RequiredSkillTemplate> requiredSkills,
        IReadOnlyDictionary<string, double> candidateSkills)
    {
        var denominator =
            requiredSkills.Sum(skill => skill.Weight);

        if (denominator <= 0)
            return 0;

        var numerator = requiredSkills.Sum(
            requiredSkill =>
            {
                var key =
                    Normalize(requiredSkill.SkillName);

                var candidateScore =
                    candidateSkills.TryGetValue(
                        key,
                        out var value)
                        ? value
                        : 0d;

                return requiredSkill.Weight
                    * candidateScore;
            });

        var readiness = numerator / denominator;

        // Round half up: 40.5 -> 41.
        return (int)Math.Clamp(
            Math.Floor(readiness + 0.5d),
            0,
            100);
    }

    private static string BuildMatchNote(
        int score,
        int matchedCount,
        int requiredCount)
    {
        if (requiredCount <= 0)
        {
            return "This role has no required skills "
                + "and was excluded from scoring.";
        }

        return $"Role readiness is {score}%. "
            + $"Matched {matchedCount} of {requiredCount} "
            + "required skills. "
            + "The score uses Σ(wᵢ × sᵢ) / Σ(wᵢ).";
    }

    private static string BuildTitle(
        string requiredJob,
        string? seniority)
    {
        return string.IsNullOrWhiteSpace(seniority)
            ? requiredJob.Trim()
            : $"{seniority.Trim()} {requiredJob.Trim()}";
    }

    private static string GetLogoLetter(
        string requiredJob)
    {
        var trimmed = requiredJob.Trim();

        return string.IsNullOrWhiteSpace(trimmed)
            ? "J"
            : trimmed[0]
                .ToString()
                .ToUpperInvariant();
    }

    private static string GetScoreColor(int score)
    {
        return score switch
        {
            >= 85 => "#10B981",
            >= 70 => "#6D5EF2",
            >= 50 => "#F59E0B",
            _ => "#EF4444"
        };
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    private sealed class RequiredSkillTemplate
    {
        public string SkillName { get; set; } =
            string.Empty;

        public int Weight { get; set; }
    }
}
