namespace GloryLikeWebApp.Models.Dashboard;

public class CandidateDashboardViewModel
{
    public string DisplayName { get; set; } = "Candidate";
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public int OverallScore { get; set; }
    public int ProfileCompletion { get; set; }

    public string StrongestRole { get; set; } = "Product Designer";
    public string StrongestRoleSubtitle { get; set; } = "Your highest readiness target";

    public List<DashboardStatItem> Stats { get; set; } = new();
    public List<DashboardApplicationItem> Applications { get; set; } = new();
    public List<DashboardJobItem> RecommendedJobs { get; set; } = new();
    public List<DashboardSkillItem> Skills { get; set; } = new();
}

public class DashboardStatItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
}

public class DashboardApplicationItem
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusClass { get; set; } = "neutral";
    public string UpdatedText { get; set; } = string.Empty;
}

public class DashboardJobItem
{
    public string Company { get; set; } = string.Empty;
    public string CompanyInitials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Meta { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class DashboardSkillItem
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Knowledge { get; set; }
    public int Experience { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusClass { get; set; } = "verified";
}
