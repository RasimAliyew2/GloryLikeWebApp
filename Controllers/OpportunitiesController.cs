using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployeePolicy)]
public sealed class OpportunitiesController : Controller
{
    private readonly ILogger<OpportunitiesController> _logger;
    private readonly IVacancyApiService _vacancyApiService;

    public OpportunitiesController(
        ILogger<OpportunitiesController> logger,
        IVacancyApiService vacancyApiService)
    {
        _logger = logger;
        _vacancyApiService = vacancyApiService;
    }

    [HttpGet("/Opportunities")]
    public async Task<IActionResult> OpportunitiesPage(
        string? search,
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Challenge();

        var model = CreateBaseModel(candidateUserId, search);

        try
        {
            var result = await _vacancyApiService.GetCandidateVacanciesAsync(
                candidateUserId,
                cancellationToken);

            if (!result.Success || result.Data is null)
            {
                model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Vacancies SQL datası yüklənmədi."
                    : result.Message;

                return View("OpportunitiesPage", model);
            }

            var data = result.Data;
            model.CurrentJobName = ResolveCurrentJobName(data);
            var opportunities = BuildOpportunities(data.Vacancies);

            if (!string.IsNullOrWhiteSpace(model.SearchText))
            {
                opportunities = opportunities
                    .Where(opportunity => opportunity.SearchText.Contains(
                        model.SearchText,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            model.Opportunities = opportunities;

            if (model.Opportunities.Count == 0)
            {
                model.EmptyMessage = string.IsNullOrWhiteSpace(model.SearchText)
                    ? string.IsNullOrWhiteSpace(data.Message)
                        ? "JobFamilyId-nizə uyğun aktiv vacancy tapılmadı."
                        : data.Message
                    : $"“{model.SearchText}” axtarışına uyğun vacancy tapılmadı.";
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Candidate vacancies could not be loaded for user {UserId}.",
                candidateUserId);

            model.ErrorMessage =
                "Vacancies SQL datası yüklənmədi: " + exception.Message;
        }

        return View("OpportunitiesPage", model);
    }

    [HttpPost("/Opportunities/{vacancyId:int}/Apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        int vacancyId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Unauthorized(new { success = false, message = "Sign in tələb olunur." });

        var result = await _vacancyApiService.ApplyAsync(
            vacancyId,
            candidateUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? "Müraciət SQL-də saxlanmadı."
                    : result.Message
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Data.Message,
            applicationId = result.Data.ApplicationId,
            status = result.Data.Status,
            statusText = "No response yet",
            appliedAtUtc = result.Data.AppliedAtUtc,
            alreadyApplied = result.Data.AlreadyApplied
        });
    }

    private bool TryGetCandidateUserId(out int candidateUserId)
    {
        return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out candidateUserId)
            && candidateUserId > 0;
    }

    private OpportunitiesPageViewModel CreateBaseModel(
        int userId,
        string? search)
    {
        var firstName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var surname = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var userName = User.FindFirstValue("username") ?? string.Empty;
        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

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
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            SearchText = search?.Trim() ?? string.Empty
        };
    }

    private static string ResolveCurrentJobName(
        CandidateVacancyListApiResponse response)
    {
        var names = response.CandidateJobFamilyNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        if (names.Count == 0)
        {
            names = response.Vacancies
                .Where(vacancy =>
                    !string.IsNullOrWhiteSpace(vacancy.JobFamilyName))
                .Select(vacancy => vacancy.JobFamilyName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();
        }

        return string.Join(", ", names);
    }

    private static List<OpportunityItem> BuildOpportunities(
        IReadOnlyCollection<CandidateVacancyApiItem> vacancies)
    {
        var result = new List<OpportunityItem>();
        var index = 0;

        foreach (var vacancy in vacancies.Where(item => item.VacancyId > 0))
        {
            var requiredSkills = vacancy.Skills
                .Where(skill => !string.IsNullOrWhiteSpace(skill.SkillName))
                .GroupBy(
                    skill => skill.SkillId > 0
                        ? $"id:{skill.SkillId}"
                        : $"name:{skill.SkillName.Trim().ToLowerInvariant()}")
                .Select(group => group
                    .OrderByDescending(skill => skill.Weight)
                    .First())
                .OrderByDescending(skill => skill.Weight)
                .ThenBy(skill => skill.SkillName)
                .ToList();

            var matchedSkills = requiredSkills
                .Where(skill => skill.IsMatched)
                .Select(skill => skill.SkillName.Trim())
                .ToList();
            var missingSkills = requiredSkills
                .Where(skill => !skill.IsMatched)
                .Select(skill => skill.SkillName.Trim())
                .ToList();
            var responsibilities = requiredSkills
                .Take(6)
                .Select(skill =>
                    $"{skill.SkillName.Trim()} — {skill.RequirementType}, weight {skill.Weight}")
                .ToList();

            var title = string.IsNullOrWhiteSpace(vacancy.RoleTitle)
                ? string.IsNullOrWhiteSpace(vacancy.PositionName)
                    ? $"Vacancy #{vacancy.VacancyId}"
                    : vacancy.PositionName.Trim()
                : vacancy.RoleTitle.Trim();
            var jobFamilyName = string.IsNullOrWhiteSpace(vacancy.JobFamilyName)
                ? "Vacancy"
                : vacancy.JobFamilyName.Trim();

            result.Add(new OpportunityItem
            {
                Id = vacancy.VacancyId,
                PlatformVacancyId = vacancy.PlatformVacancyId,
                LogoLetter = GetLogoLetter(title),
                Company = jobFamilyName,
                EmployerName = vacancy.EmployerName,
                Title = title,
                Level = vacancy.SeniorityName,
                Location = string.IsNullOrWhiteSpace(vacancy.EmployerName)
                    ? "Employer vacancy"
                    : vacancy.EmployerName.Trim(),
                WorkType = vacancy.EmploymentType,
                Salary = BuildSalaryText(vacancy),
                Score = Math.Clamp(vacancy.MatchScore, 0, 100),
                ScoreColor = GetScoreColor(vacancy.MatchScore),
                IsExpanded = index == 0,
                AboutRole = string.IsNullOrWhiteSpace(vacancy.JobDescription)
                    ? "Bu vacancy üçün job description qeyd edilməyib."
                    : vacancy.JobDescription.Trim(),
                Responsibilities = string.Join(
                    Environment.NewLine,
                    responsibilities),
                MatchedSkills = matchedSkills.Count == 0
                    ? "No matched skills yet"
                    : string.Join(", ", matchedSkills),
                MissingSkills = missingSkills.Count == 0
                    ? "No missing required skills"
                    : string.Join(", ", missingSkills),
                MatchNote = BuildMatchNote(
                    vacancy.MatchScore,
                    matchedSkills.Count,
                    requiredSkills.Count),
                RequiredSkillsCount = requiredSkills.Count,
                RequiredSkillItems = requiredSkills
                    .Select(skill => skill.SkillName.Trim())
                    .ToList(),
                MatchedSkillItems = matchedSkills,
                MissingSkillItems = missingSkills,
                ResponsibilityItems = responsibilities,
                IsApplied = vacancy.HasApplied,
                ApplicationId = vacancy.ApplicationId,
                ApplicationStatus = vacancy.ApplicationStatus,
                AppliedAtUtc = vacancy.AppliedAtUtc
            });

            index++;
        }

        return result
            .OrderByDescending(opportunity => opportunity.Score)
            .ThenBy(opportunity => opportunity.Title)
            .ToList();
    }

    private static string BuildSalaryText(CandidateVacancyApiItem vacancy)
    {
        if (vacancy.HideSalary
            || (!vacancy.MinSalary.HasValue && !vacancy.MaxSalary.HasValue))
        {
            return "Salary not disclosed";
        }

        var currency = string.IsNullOrWhiteSpace(vacancy.Currency)
            ? string.Empty
            : $" {vacancy.Currency.Trim()}";

        if (vacancy.MinSalary.HasValue && vacancy.MaxSalary.HasValue)
        {
            return $"{vacancy.MinSalary.Value:0.##}–{vacancy.MaxSalary.Value:0.##}{currency}";
        }

        return vacancy.MinSalary.HasValue
            ? $"From {vacancy.MinSalary.Value:0.##}{currency}"
            : $"Up to {vacancy.MaxSalary!.Value:0.##}{currency}";
    }

    private static string BuildMatchNote(
        int score,
        int matchedCount,
        int requiredCount)
    {
        if (requiredCount == 0)
            return "Job uyğundur; vacancy skill template-i boşdur.";

        return $"Role readiness is {Math.Clamp(score, 0, 100)}%. "
            + $"Matched {matchedCount} of {requiredCount} required skills.";
    }

    private static string GetLogoLetter(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? "V"
            : char.ToUpperInvariant(trimmed[0]).ToString();
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
}
