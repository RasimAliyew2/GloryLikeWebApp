namespace GloryLikeWebApp.Models;

public class SkillsPageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Candidate";
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public List<UserSkillInfo> Skills { get; set; } = new();
    public List<UserWorkExperienceInfo> WorkExperiences { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSkills => Skills.Count > 0;
    public bool HasExperiences => WorkExperiences.Count > 0;

    public int VerifiedSkillsCount =>
        Skills.Count(x =>
            x.IsVerified ||
            string.Equals(x.Status, "verified", StringComparison.OrdinalIgnoreCase));

    public int AverageCredibility =>
        Skills.Count == 0
            ? 0
            : (int)Math.Floor(Skills.Average(x => x.CalculatedCredibilityScore) + 0.5d);
}
