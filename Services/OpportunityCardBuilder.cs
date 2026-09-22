using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public static class OpportunityCardBuilder
{
    public static List<OpportunityItem> Build(
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
                Location = string.IsNullOrWhiteSpace(vacancy.LocationName)
                    ? "Location not specified"
                    : vacancy.LocationName.Trim(),
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
