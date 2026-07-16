namespace GloryLikeWebApp.Models;

public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeniorityId { get; set; }
    public List<Skill> Skills { get; set; } = new();
}
