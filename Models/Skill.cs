namespace GloryLikeWebApp.Models;

public class Skill
{
    public int Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public string? SkillComplexity { get; set; }
    public int MinimumSenioritySortOrder { get; set; } = 1;
    public bool IsCore { get; set; }
    public string AssessmentType { get; set; } = "TP";
    public string VerificationMethod { get; set; } = string.Empty;
}
