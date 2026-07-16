namespace GloryLikeWebApp.Models;

public class Seniority
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int JobFamilyId { get; set; }
    public List<Position> Positions { get; set; } = new();
}
