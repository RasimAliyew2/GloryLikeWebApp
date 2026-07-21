using System.Diagnostics;
using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Dashboard;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployeePolicy)]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUserProfileDataApiService _userProfileDataApiService;
    private readonly IVacancyApiService _vacancyApiService;

    public HomeController(
        ILogger<HomeController> logger,
        IUserProfileDataApiService userProfileDataApiService,
        IVacancyApiService vacancyApiService)
    {
        _logger = logger;
        _userProfileDataApiService = userProfileDataApiService;
        _vacancyApiService = vacancyApiService;
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
            Email = email
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
                "A Job is required before vacancies can be matched.";

            BuildStats(
                model,
                averageCredibility,
                verifiedCount);

            return View(model);
        }

        try
        {
            var vacancyResult =
                await _vacancyApiService.GetCandidateVacanciesAsync(
                    userId,
                    cancellationToken);

            if (!vacancyResult.Success || vacancyResult.Data is null)
            {
                model.RecommendedJobsError =
                    string.IsNullOrWhiteSpace(vacancyResult.Message)
                        ? "Recommended vacancies could not be loaded."
                        : vacancyResult.Message;
                model.RecommendedJobsEmptyMessage =
                    "SQL Vacancies data could not be loaded.";

                BuildStats(
                    model,
                    averageCredibility,
                    verifiedCount);

                return View(model);
            }

            model.RecommendedJobs = BuildRecommendedJobs(
                vacancyResult.Data.Vacancies);
            model.Applications = BuildDashboardApplications(
                vacancyResult.Data.Vacancies);

            if (model.RecommendedJobs.Count > 0)
            {
                var bestMatch = model.RecommendedJobs[0];

                model.OverallScore = bestMatch.Score;
                model.StrongestRole = bestMatch.Title;
                model.StrongestRoleSubtitle =
                    $"{currentJobName} · best live vacancy match";
            }
            else
            {
                model.RecommendedJobsEmptyMessage =
                    $"SQL-də “{currentJobName}” Job-u üçün uyğun aktiv Vacancy tapılmadı.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Recommended jobs could not be loaded for user {UserId}.",
                userId);

            model.RecommendedJobsError =
                "Recommended Vacancies could not be loaded: " +
                ex.Message;
        }

        BuildStats(
            model,
            averageCredibility,
            verifiedCount);

        return View(model);
    }

    private static List<RecommendedJobItem> BuildRecommendedJobs(
        IReadOnlyCollection<CandidateVacancyApiItem> vacancies)
    {
        return vacancies
            .Where(vacancy => vacancy.VacancyId > 0)
            .Select(vacancy =>
            {
                var title = string.IsNullOrWhiteSpace(vacancy.RoleTitle)
                    ? string.IsNullOrWhiteSpace(vacancy.PositionName)
                        ? $"Vacancy #{vacancy.VacancyId}"
                        : vacancy.PositionName.Trim()
                    : vacancy.RoleTitle.Trim();
                var skills = vacancy.Skills
                    .Where(skill =>
                        !string.IsNullOrWhiteSpace(skill.SkillName))
                    .GroupBy(skill => skill.SkillId > 0
                        ? $"id:{skill.SkillId}"
                        : $"name:{skill.SkillName.Trim().ToLowerInvariant()}")
                    .Select(group => group
                        .OrderByDescending(skill => skill.Weight)
                        .First())
                    .ToList();

                return new RecommendedJobItem
                {
                    Id = vacancy.VacancyId,
                    LogoLetter = GetLogoLetter(title),
                    Company = ResolveEmployerName(vacancy),
                    Title = title,
                    Description = string.IsNullOrWhiteSpace(
                        vacancy.JobDescription)
                        ? "Employer vacancy matching your Job."
                        : vacancy.JobDescription.Trim(),
                    Level = string.Join(
                        " · ",
                        new[]
                        {
                            vacancy.SeniorityName,
                            vacancy.JobFamilyName
                        }.Where(value =>
                            !string.IsNullOrWhiteSpace(value))),
                    Score = Math.Clamp(vacancy.MatchScore, 0, 100),
                    RequiredSkillsCount = skills.Count,
                    MatchedSkills = skills
                        .Where(skill => skill.IsMatched)
                        .Select(skill => skill.SkillName.Trim())
                        .ToList(),
                    MissingSkills = skills
                        .Where(skill => !skill.IsMatched)
                        .Select(skill => skill.SkillName.Trim())
                        .ToList()
                };
            })
            .OrderByDescending(vacancy => vacancy.Score)
            .ThenBy(vacancy => vacancy.Title)
            .Take(5)
            .ToList();
    }

    private static List<DashboardApplicationItem> BuildDashboardApplications(
        IReadOnlyCollection<CandidateVacancyApiItem> vacancies)
    {
        return vacancies
            .Where(vacancy => vacancy.HasApplied)
            .OrderByDescending(vacancy => vacancy.AppliedAtUtc)
            .Take(5)
            .Select(vacancy => new DashboardApplicationItem
            {
                Company = ResolveEmployerName(vacancy),
                Role = string.IsNullOrWhiteSpace(vacancy.RoleTitle)
                    ? vacancy.PositionName
                    : vacancy.RoleTitle,
                Status = "No response yet",
                StatusClass = "review",
                UpdatedText = vacancy.AppliedAtUtc.HasValue
                    ? $"Applied {vacancy.AppliedAtUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm}"
                    : "Applied"
            })
            .ToList();
    }

    private static string ResolveEmployerName(
        CandidateVacancyApiItem vacancy)
    {
        if (!string.IsNullOrWhiteSpace(vacancy.EmployerName))
            return vacancy.EmployerName.Trim();

        return string.IsNullOrWhiteSpace(vacancy.JobFamilyName)
            ? "Employer"
            : vacancy.JobFamilyName.Trim();
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
                Caption = "Best SQL vacancy"
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

}
