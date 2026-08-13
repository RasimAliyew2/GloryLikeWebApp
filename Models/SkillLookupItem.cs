namespace GloryLikeWebApp.Models;

public sealed class SkillLookupItem
{
    public int Id { get; set; }
    public string SkillName { get; set; } = string.Empty;

    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;

    public List<SeniorityOption> Seniorities { get; set; } = new();

    public string SkillComplexity { get; set; } = "medium";
}

public sealed class SeniorityOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
