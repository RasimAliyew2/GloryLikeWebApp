using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Models.Dashboard;

public class CandidateDashboardViewModel
{
    public string DisplayName { get; set; } = "Candidate";
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentJobName { get; set; } = string.Empty;

    public int OverallScore { get; set; }
    public int ProfileCompletion { get; set; }

    public string StrongestRole { get; set; } = "No role yet";
    public string StrongestRoleSubtitle { get; set; } =
        "Add your Job and skills to calculate readiness.";

    public List<DashboardStatItem> Stats { get; set; } = new();
    public List<DashboardApplicationItem> Applications { get; set; } = new();
    public List<RecommendedJobItem> RecommendedJobs { get; set; } = new();
    public List<DashboardSkillItem> Skills { get; set; } = new();

    public string? RecommendedJobsError { get; set; }
    public string RecommendedJobsEmptyMessage { get; set; } =
        "No matching active vacancies were found for your Job.";

    public bool HasRecommendedJobs => RecommendedJobs.Count > 0;
    public bool HasRecommendedJobsError =>
        !string.IsNullOrWhiteSpace(RecommendedJobsError);

    public string RecommendedJobsCountText =>
        RecommendedJobs.Count == 0
            ? "No matching roles"
            : RecommendedJobs.Count == 1
                ? "1 role for you"
                : $"{RecommendedJobs.Count} roles for you";
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
