namespace GloryLikeWebApp.Models.Employer;

public sealed class EmployerHomeViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;

    public List<EmployerDashboardStatItem> Stats { get; set; } = new();
    public List<EmployerInsightItem> Insights { get; set; } = new();
    public List<EmployerCandidateItem> Candidates { get; set; } = new();

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName)
                ? Email
                : DisplayName;

            var parts = source
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();

            if (parts.Count == 0)
                return "EM";

            return string.Concat(
                parts.Select(part => char.ToUpperInvariant(part[0])));
        }
    }
}

public sealed class EmployerDashboardStatItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string AccentClass { get; set; } = string.Empty;
}

public sealed class EmployerInsightItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public sealed class EmployerCandidateItem
{
    public string Name { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    public string CurrentCompany { get; set; } = string.Empty;

    public int TrustScore { get; set; }
    public int MatchScore { get; set; }

    public List<string> Signals { get; set; } = new();

    public string Initials
    {
        get
        {
            var parts = Name
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2);

            var value = string.Concat(
                parts.Select(part => char.ToUpperInvariant(part[0])));

            return string.IsNullOrWhiteSpace(value)
                ? "C"
                : value;
        }
    }
}
