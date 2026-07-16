using System.Diagnostics;
using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Dashboard;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUserProfileDataApiService _userProfileDataApiService;
    private readonly IJobOffersApiService _jobOffersApiService;

    public HomeController(
        ILogger<HomeController> logger,
        IUserProfileDataApiService userProfileDataApiService,
        IJobOffersApiService jobOffersApiService)
    {
        _logger = logger;
        _userProfileDataApiService = userProfileDataApiService;
        _jobOffersApiService = jobOffersApiService;
    }

    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId) ||
            userId <= 0)
        {
            return Challenge();
        }

        var firstName = User.FindFirstValue(
            ClaimTypes.Name)
            ?? string.Empty;

        var surname = User.FindFirstValue(
            ClaimTypes.Surname)
            ?? string.Empty;

        var userName = User.FindFirstValue("username")
            ?? string.Empty;

        var email = User.FindFirstValue(
            ClaimTypes.Email)
            ?? string.Empty;

        var phone = User.FindFirstValue(
            ClaimTypes.MobilePhone)
            ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = string.IsNullOrWhiteSpace(userName)
                ? "Candidate"
                : userName;
        }

        var model = new CandidateDashboardViewModel
        {
            DisplayName = displayName,
            UserName = userName,
            Email = email,

            // Applications still use seed data because there is no
            // applications API in the current BackendApp.
            Applications =
            {
                new DashboardApplicationItem
                {
                    Company = "Northstar",
                    Role = "Senior Product Designer",
                    Status = "Interview",
                    StatusClass = "interview",
                    UpdatedText = "Updated 2h ago"
                },
                new DashboardApplicationItem
                {
                    Company = "Vertex Labs",
                    Role = "Product Designer",
                    Status = "In review",
                    StatusClass = "review",
                    UpdatedText = "Updated yesterday"
                }
            }
        };

        var profileResult =
            await _userProfileDataApiService.GetAsync(
                userId,
                cancellationToken);

        var selectedSkills =
            profileResult.Success &&
            profileResult.Data is not null
                ? profileResult.Data.Skills
                    ?? new List<UserSkillInfo>()
                : new List<UserSkillInfo>();

        var experiences =
            profileResult.Success &&
            profileResult.Data is not null
                ? profileResult.Data.Experiences
                    ?? new List<UserWorkExperienceInfo>()
                : new List<UserWorkExperienceInfo>();

        var currentJobName = ResolveCurrentJobName(
            selectedSkills);

        model.CurrentJobName = currentJobName;
        model.StrongestRole = string.IsNullOrWhiteSpace(
            currentJobName)
            ? "No Job selected"
            : currentJobName;

        model.ProfileCompletion = CalculateProfileCompletion(
            displayName,
            userName,
            email,
            phone,
            currentJobName,
            selectedSkills,
            experiences);

        model.Skills = BuildTopSkills(
            selectedSkills);

        var averageCredibility = selectedSkills.Count == 0
            ? 0
            : RoundHalfUp(
                selectedSkills.Average(
                    x => x.CalculatedCredibilityScore));

        var verifiedCount = selectedSkills.Count(IsVerified);

        if (!profileResult.Success)
        {
            model.RecommendedJobsError =
                profileResult.Message;

            model.RecommendedJobsEmptyMessage =
                "SQL profile data could not be loaded.";

            BuildStats(
                model,
                averageCredibility,
                verifiedCount);

            return View(model);
        }

        if (string.IsNullOrWhiteSpace(currentJobName))
        {
            model.RecommendedJobsEmptyMessage =
                "Select a Job and add at least one related skill. Recommendations are filtered by Job before scoring.";

            model.StrongestRoleSubtitle =
                "A Job is required before offers can be matched.";

            BuildStats(
                model,
                averageCredibility,
                verifiedCount);

            return View(model);
        }

        try
        {
            var offers =
                await _jobOffersApiService.GetJobOffersAsync(
                    cancellationToken);

            model.RecommendedJobs = BuildRecommendedJobs(
                offers,
                currentJobName,
                selectedSkills);

            if (model.RecommendedJobs.Count > 0)
            {
                var bestMatch = model.RecommendedJobs[0];

                model.OverallScore = bestMatch.Score;
                model.StrongestRole = bestMatch.Title;
                model.StrongestRoleSubtitle =
                    $"{currentJobName} · best SQL job-offer match";
            }
            else
            {
                model.RecommendedJobsEmptyMessage =
                    $"SQL-də “{currentJobName}” Job-u üçün uyğun JobOffer tapılmadı.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Recommended jobs could not be loaded for user {UserId}.",
                userId);

            model.RecommendedJobsError =
                "Recommended JobOffers could not be loaded: " +
                ex.Message;
        }

        BuildStats(
            model,
            averageCredibility,
            verifiedCount);

        return View(model);
    }

    private static List<RecommendedJobItem> BuildRecommendedJobs(
        IReadOnlyList<JobOfferApiItem> offers,
        string currentJobName,
        IReadOnlyCollection<UserSkillInfo> selectedSkills)
    {
        var candidateSkills = selectedSkills
            .Where(
                x => !string.IsNullOrWhiteSpace(
                    x.SkillName))
            .GroupBy(
                x => Normalize(x.SkillName),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Max(GetSkillSignal),
                StringComparer.OrdinalIgnoreCase);

        // The Job filter is applied before any score is calculated.
        // An unrelated JobOffer can never enter the recommendation list.
        var matchingOffers = offers
            .Where(x =>
                x is not null &&
                !string.IsNullOrWhiteSpace(x.RequiredJob) &&
                !string.IsNullOrWhiteSpace(x.Skills) &&
                x.SkillsWeight > 0 &&
                Normalize(x.RequiredJob) ==
                Normalize(currentJobName))
            .ToList();

        var groups = matchingOffers
            .GroupBy(x => new
            {
                RequiredJob = x.RequiredJob.Trim(),
                Name = string.IsNullOrWhiteSpace(x.Name)
                    ? BuildTitle(
                        x.RequiredJob,
                        x.Seniority)
                    : x.Name.Trim(),
                Description =
                    x.Description?.Trim()
                    ?? string.Empty,
                Seniority =
                    string.IsNullOrWhiteSpace(
                        x.Seniority)
                        ? "Middle"
                        : x.Seniority.Trim()
            })
            .ToList();

        var result = new List<RecommendedJobItem>();

        foreach (var group in groups)
        {
            var requiredSkills = group
                .Select(x => new RequiredSkillItem
                {
                    SkillName = x.Skills.Trim(),
                    Weight = Math.Max(
                        x.SkillsWeight,
                        1)
                })
                .Where(
                    x => !string.IsNullOrWhiteSpace(
                        x.SkillName))
                .GroupBy(
                    x => Normalize(x.SkillName),
                    StringComparer.OrdinalIgnoreCase)
                .Select(skillGroup =>
                    new RequiredSkillItem
                    {
                        SkillName =
                            skillGroup.First()
                                .SkillName,
                        Weight =
                            skillGroup.Max(
                                x => x.Weight)
                    })
                .ToList();

            if (requiredSkills.Count == 0)
                continue;

            var score = CalculateRoleReadiness(
                requiredSkills,
                candidateSkills);

            var matchedSkills = requiredSkills
                .Where(x =>
                    candidateSkills.ContainsKey(
                        Normalize(x.SkillName)))
                .Select(x => x.SkillName)
                .OrderBy(x => x)
                .ToList();

            var missingSkills = requiredSkills
                .Where(x =>
                    !candidateSkills.ContainsKey(
                        Normalize(x.SkillName)))
                .Select(x => x.SkillName)
                .OrderBy(x => x)
                .ToList();

            result.Add(new RecommendedJobItem
            {
                Id = group.Min(x => x.Id),
                LogoLetter = GetLogoLetter(
                    group.Key.RequiredJob),
                Company = group.Key.RequiredJob,
                Title = group.Key.Name,
                Description =
                    string.IsNullOrWhiteSpace(
                        group.Key.Description)
                        ? $"Matched against {requiredSkills.Count} required skill(s)."
                        : group.Key.Description,
                Level = group.Key.Seniority,
                Score = score,
                RequiredSkillsCount =
                    requiredSkills.Count,
                MatchedSkills = matchedSkills,
                MissingSkills = missingSkills
            });
        }

        return result
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Title)
            .Take(5)
            .ToList();
    }

    private static int CalculateRoleReadiness(
        IReadOnlyCollection<RequiredSkillItem> requiredSkills,
        IReadOnlyDictionary<string, double> candidateSkills)
    {
        var denominator = requiredSkills.Sum(
            x => x.Weight);

        if (denominator <= 0)
            return 0;

        var numerator = requiredSkills.Sum(x =>
        {
            var key = Normalize(x.SkillName);

            var signal = candidateSkills.TryGetValue(
                key,
                out var value)
                ? value
                : 0d;

            return x.Weight * signal;
        });

        return RoundHalfUp(
            numerator / denominator);
    }

    private static double GetSkillSignal(
        UserSkillInfo skill)
    {
        // UserSkillInfo.Signal follows the same rule as Mobile App:
        // verified => credibility,
        // self_declared => min(credibility, 40),
        // absent => 0.
        return Math.Clamp(
            skill.Signal,
            0d,
            100d);
    }

    private static string ResolveCurrentJobName(
        IReadOnlyCollection<UserSkillInfo> selectedSkills)
    {
        var byClaim = string.Empty;

        // Existing or future authentication code can store this claim.
        // SQL-saved skill JobFamilyName remains the fallback used today.
        // This keeps recommendation filtering deterministic.
        if (selectedSkills.Count == 0)
            return byClaim;

        return selectedSkills
            .Where(
                x => !string.IsNullOrWhiteSpace(
                    x.JobFamilyName))
            .GroupBy(
                x => x.JobFamilyName.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key)
            .Select(x => x.Key)
            .FirstOrDefault()
            ?? string.Empty;
    }

    private static List<DashboardSkillItem> BuildTopSkills(
        IReadOnlyCollection<UserSkillInfo> selectedSkills)
    {
        return selectedSkills
            .Where(
                x => !string.IsNullOrWhiteSpace(
                    x.SkillName))
            .OrderByDescending(
                x => GetSkillSignal(x))
            .ThenByDescending(
                x => x.CalculatedCredibilityScore)
            .ThenBy(x => x.SkillName)
            .Take(3)
            .Select(x => new DashboardSkillItem
            {
                Name = x.SkillName,
                Category =
                    string.IsNullOrWhiteSpace(
                        x.JobFamilyName)
                        ? "Skill"
                        : x.JobFamilyName,
                Score = RoundHalfUp(
                    x.CalculatedCredibilityScore),
                Knowledge = RoundHalfUp(
                    x.KnowledgeScore),
                Experience = RoundHalfUp(
                    x.ExperienceScore),
                Status = x.StatusText,
                StatusClass = IsVerified(x)
                    ? "verified"
                    : "review"
            })
            .ToList();
    }

    private static int CalculateProfileCompletion(
        string displayName,
        string userName,
        string email,
        string phone,
        string currentJobName,
        IReadOnlyCollection<UserSkillInfo> skills,
        IReadOnlyCollection<UserWorkExperienceInfo> experiences)
    {
        var score = 0;

        if (!string.IsNullOrWhiteSpace(displayName) &&
            !displayName.Equals(
                "Candidate",
                StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(userName))
            score += 10;

        if (!string.IsNullOrWhiteSpace(email))
            score += 10;

        if (!string.IsNullOrWhiteSpace(phone))
            score += 10;

        if (!string.IsNullOrWhiteSpace(
                currentJobName))
        {
            score += 15;
        }

        if (skills.Count > 0)
            score += 20;

        if (experiences.Count > 0)
            score += 10;

        if (skills.Any(IsVerified))
            score += 5;

        return Math.Clamp(score, 0, 100);
    }

    private static void BuildStats(
        CandidateDashboardViewModel model,
        int averageCredibility,
        int verifiedCount)
    {
        model.Stats.Clear();

        model.Stats.Add(
            new DashboardStatItem
            {
                Label = "Match",
                Value = model.OverallScore.ToString(),
                Caption = "Best SQL offer"
            });

        model.Stats.Add(
            new DashboardStatItem
            {
                Label = "Trust",
                Value = averageCredibility.ToString(),
                Caption = "Avg. credibility"
            });

        model.Stats.Add(
            new DashboardStatItem
            {
                Label = "Verified",
                Value = verifiedCount.ToString(),
                Caption = "Trusted skills"
            });

        model.Stats.Add(
            new DashboardStatItem
            {
                Label = "Strength",
                Value = $"{model.OverallScore}%",
                Caption = "Overall match"
            });
    }

    private static bool IsVerified(
        UserSkillInfo skill)
    {
        return skill.IsVerified ||
               string.Equals(
                   skill.Status,
                   "verified",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTitle(
        string requiredJob,
        string seniority)
    {
        return string.IsNullOrWhiteSpace(seniority)
            ? requiredJob.Trim()
            : $"{seniority.Trim()} {requiredJob.Trim()}";
    }

    private static string GetLogoLetter(
        string value)
    {
        var trimmed = value.Trim();

        return string.IsNullOrWhiteSpace(trimmed)
            ? "J"
            : trimmed[0]
                .ToString()
                .ToUpperInvariant();
    }

    private static string Normalize(
        string value)
    {
        value = (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();

        while (value.Contains(
                   "  ",
                   StringComparison.Ordinal))
        {
            value = value.Replace(
                "  ",
                " ",
                StringComparison.Ordinal);
        }

        return value;
    }

    private static int RoundHalfUp(
        double value)
    {
        if (double.IsNaN(value) ||
            double.IsInfinity(value))
        {
            return 0;
        }

        return (int)Math.Clamp(
            Math.Floor(value + 0.5d),
            0,
            100);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
    }

    private sealed class RequiredSkillItem
    {
        public string SkillName { get; set; } =
            string.Empty;

        public int Weight { get; set; }
    }
}
