namespace GloryLikeWebApp.Models;

public sealed class SkillLookupItem
{
    public int Id { get; set; }
    public string SkillName { get; set; } = string.Empty;

    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;

    public int SeniorityId { get; set; }
    public string SeniorityName { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;

    public string SkillComplexity { get; set; } = "medium";
}
