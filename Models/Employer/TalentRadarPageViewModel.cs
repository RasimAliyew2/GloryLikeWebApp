namespace GloryLikeWebApp.Models.Employer;

public sealed class TalentRadarPageViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public int TotalVacancies { get; set; }
    public int ScoredVacancies { get; set; }
    public List<TalentRadarCandidateViewModel> Candidates { get; set; } = new();

    public string Initials => BuildInitials(DisplayName, "EM");

    private static string BuildInitials(
        string? value,
        string fallback)
    {
        var parts = (value ?? string.Empty)
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .ToList();

        return parts.Count == 0
            ? fallback
            : string.Concat(parts.Select(
                part => char.ToUpperInvariant(part[0])));
    }
}

public sealed class TalentRadarCandidateViewModel
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public int BestVacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string TargetRoleTitle { get; set; } = string.Empty;
    public int RoleReadiness { get; set; }
    public int MatchedVacancyCount { get; set; }
    public int MatchedSkillsCount { get; set; }
    public int TemplateSkillsCount { get; set; }
    public List<TalentRadarSkillViewModel> Skills { get; set; } = new();

    public string Initials
    {
        get
        {
            var parts = Name
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();

            return parts.Count == 0
                ? "CA"
                : string.Concat(parts.Select(
                    part => char.ToUpperInvariant(part[0])));
        }
    }

    public string ScoreClass => RoleReadiness >= 80
        ? "high"
        : RoleReadiness >= 60
            ? "medium"
            : "low";
}

public sealed class TalentRadarSkillViewModel
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsVerified { get; set; }
}
