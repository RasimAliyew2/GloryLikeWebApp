namespace GloryLikeWebApp.Models;

public class JobFamily
{
    public int Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public List<Seniority> Seniorities { get; set; } = new();
}
