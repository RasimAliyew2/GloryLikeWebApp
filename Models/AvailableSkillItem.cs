namespace GloryLikeWebApp.Models;

public class AvailableSkillItem
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;

    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;

    public int SeniorityId { get; set; }
    public string SeniorityName { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;

    public string SkillComplexity { get; set; } = "medium";

    public string SelectionKey =>
        $"{JobFamilyId}:{SeniorityId}:{PositionId}:{SkillId}";

    public string DisplayName
    {
        get
        {
            var context = string.Join(
                " · ",
                new[] { PositionName, SeniorityName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            return string.IsNullOrWhiteSpace(context)
                ? SkillName
                : $"{SkillName} — {context}";
        }
    }
}
