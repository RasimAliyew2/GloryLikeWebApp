using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Student;

namespace GloryLikeWebApp.Services;

public static class StudentDashboardBuilder
{
    public static bool IsInternship(string? employmentType) =>
        string.Equals(employmentType?.Trim(), "Internship", StringComparison.OrdinalIgnoreCase);

    public static bool IsStudentOpportunity(CandidateVacancyApiItem vacancy)
    {
        if (IsInternship(vacancy.EmploymentType)) return true;
        var seniority = vacancy.SeniorityName?.Trim().ToLowerInvariant() ?? "";
        return seniority is "intern" or "internship" or "junior" or "entry" or "entry level" or "entry-level" or "trainee";
    }

    public static int RoundSignal(double value) => double.IsFinite(value)
        ? (int)Math.Clamp(Math.Floor(value + 0.5d), 0, 100) : 0;

    public static void Populate(StudentDashboardViewModel model, IEnumerable<UserSkillInfo> skills,
        IEnumerable<CandidateVacancyApiItem> vacancies, IEnumerable<CandidateApplicationApiItem> applications)
    {
        // A skill can be attached to several jobs: count its strongest saved signal once.
        model.Skills = skills.Where(s => s.SkillId > 0 && !string.IsNullOrWhiteSpace(s.SkillName))
            .GroupBy(s => s.SkillId).Select(g => g.OrderByDescending(s => double.IsFinite(s.Signal) ? s.Signal : 0)
                .ThenByDescending(s => s.IsVerified).First()).OrderByDescending(s => s.Signal).ToList();
        model.Ssi = model.Skills.Count == 0 ? 0 : RoundSignal(model.Skills.Average(s => double.IsFinite(s.Signal) ? s.Signal : 0));
        model.Opportunities = vacancies.Where(v => v.VacancyId > 0 && IsStudentOpportunity(v))
            .GroupBy(v => v.VacancyId).Select(g => g.First())
            .OrderByDescending(v => v.MatchScore).ThenByDescending(v => v.CreatedAtUtc).ToList();
        model.InternshipApplications = applications.Where(a => IsInternship(a.EmploymentType))
            .OrderByDescending(a => a.AppliedAtUtc).ToList();
        var intern = model.InternshipApplications.Any(a => a.HiredAtUtc.HasValue);
        var count = model.Skills.Count;
        var verified = model.VerifiedSkills;
        model.Achievements =
        [
            new("First step", "Add your university and specialty", "cap", model.Education.HasEducation ? 100 : 0, model.Education.Success),
            new("Verifier ×5", "Verify 5 skills", "verified", Math.Min(100, verified * 20), model.SkillsAvailable),
            new("First internship", "Reach Hired in an internship funnel", "briefcase", intern ? 100 : 0, model.ApplicationsAvailable),
            new("Job-Ready", "Reach the Job-Ready level (SSI 61+)", "rocket", Math.Min(100, model.Ssi * 100 / 61), model.SkillsAvailable),
            new("Standout", "Reach the Standout level (SSI 81+)", "crown", Math.Min(100, model.Ssi * 100 / 81), model.SkillsAvailable),
            new("Skill explorer", "Add your first 3 skills", "spark", Math.Min(100, count * 100 / 3), model.SkillsAvailable),
            new("Skill portfolio", "Add 5 skills to your profile", "folder", Math.Min(100, count * 20), model.SkillsAvailable),
            new("Verifier ×10", "Verify 10 skills", "medal", Math.Min(100, verified * 10), model.SkillsAvailable)
        ];
    }
}
