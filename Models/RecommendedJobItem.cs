namespace GloryLikeWebApp.Models;

/// <summary>
/// Web and Mobile App use the same recommended-job presentation model.
/// </summary>
public sealed class RecommendedJobItem
{
    public int Id { get; set; }
    public string LogoLetter { get; set; } = "J";
    public string Company { get; set; } = string.Empty;
    public string PostedAgo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string WorkType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Salary { get; set; } = string.Empty;
    public int Score { get; set; }

    public int RequiredSkillsCount { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();

    public string SkillsText =>
        MatchedSkills.Count == 0
            ? "No matched skills yet"
            : string.Join(", ", MatchedSkills.Take(4));

    public string MissingSkillsText =>
        MissingSkills.Count == 0
            ? "No missing key skills"
            : string.Join(", ", MissingSkills.Take(4));

    public string Meta =>
        string.Join(
            " · ",
            new[]
            {
                string.IsNullOrWhiteSpace(Level) ? null : Level,
                RequiredSkillsCount == 1
                    ? "1 required skill"
                    : $"{RequiredSkillsCount} required skills"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
