namespace GloryLikeWebApp.Models;

public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int JobFamilyId { get; set; }
    public List<Seniority> Seniorities { get; set; } = new();
}
