namespace GloryLikeWebApp.Models;

/// <summary>
/// Backend GET /api/JobOffers response item.
/// Property names intentionally match the Mobile App contract.
/// </summary>
public sealed class JobOfferApiItem
{
    public int Id { get; set; }
    public string RequiredJob { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public int SkillsWeight { get; set; }
}
