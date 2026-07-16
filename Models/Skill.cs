namespace GloryLikeWebApp.Models;

public class Skill
{
    public int Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public string? SkillComplexity { get; set; }
}
