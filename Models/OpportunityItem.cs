namespace GloryLikeWebApp.Models;

/// <summary>
/// Mobile App-dakı OpportunityItem ilə eyni təqdimat modelidir.
/// Bütün iş məlumatları BackendApp JobOffers SQL datasından hazırlanır.
/// </summary>
public sealed class OpportunityItem
{
    public int Id { get; set; }

    public string LogoLetter { get; set; } = "J";
    public string Company { get; set; } = string.Empty;
    public string PostedAgo { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string WorkType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Salary { get; set; } = string.Empty;

    public int Score { get; set; }
    public string ScoreColor { get; set; } = "#EF4444";

    public bool IsExpanded { get; set; }
    public bool IsSaved { get; set; }

    public string AboutRole { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
    public string MatchedSkills { get; set; } = string.Empty;
    public string MissingSkills { get; set; } = string.Empty;
    public string MatchNote { get; set; } = string.Empty;

    public int RequiredSkillsCount { get; set; }

    public List<string> RequiredSkillItems { get; set; } = new();
    public List<string> MatchedSkillItems { get; set; } = new();
    public List<string> MissingSkillItems { get; set; } = new();
    public List<string> ResponsibilityItems { get; set; } = new();

    public string ScoreClass =>
        Score >= 85
            ? "excellent"
            : Score >= 70
                ? "strong"
                : Score >= 50
                    ? "medium"
                    : "low";

    public string SearchText =>
        string.Join(
            " ",
            new[]
            {
                Company,
                Title,
                AboutRole,
                Level,
                string.Join(" ", RequiredSkillItems),
                string.Join(" ", MatchedSkillItems),
                string.Join(" ", MissingSkillItems)
            });
}
