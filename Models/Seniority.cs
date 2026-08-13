namespace GloryLikeWebApp.Models;

public class Seniority
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<Skill> Skills { get; set; } = new();
}
