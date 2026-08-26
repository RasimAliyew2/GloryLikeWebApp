namespace GloryLikeWebApp.Models.Employer;

public abstract class EmployerReportsShellViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public string Initials => CompanyTeamPageViewModel.InitialsFrom(
        string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName,
        "EM");
}

public sealed class OrganizationReportsPageViewModel
    : EmployerReportsShellViewModel
{
    public static readonly string[] SupportedTabs =
    [
        "overview",
        "funnel",
        "sources",
        "hiring-time",
        "team"
    ];

    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string ActiveTab { get; set; } = "overview";
    public OrganizationAnalyticsDashboardApiResponse Dashboard { get; set; } =
        new();

    public static string NormalizeTab(string? tab)
    {
        return SupportedTabs.Contains(
            tab ?? string.Empty,
            StringComparer.OrdinalIgnoreCase)
                ? tab!.ToLowerInvariant()
                : "overview";
    }
}

public sealed class OrganizationReportsQuery
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string Tab { get; set; } = "overview";
}

public sealed class OrganizationAnalyticsDashboardApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public bool ContainsDemoData { get; set; }
    public int TotalApplications { get; set; }
    public int HiredCount { get; set; }
    public bool HiredCountIsDemo { get; set; }
    public int InProcessApplications { get; set; }
    public int AverageTimeToHireDays { get; set; }
    public bool AverageTimeToHireIsDemo { get; set; }
    public decimal AcceptedOfferRatePercent { get; set; }
    public bool AcceptedOfferRateIsDemo { get; set; }
    public int ActiveVacancies { get; set; }
    public List<ReportsMonthlyActivityViewModel> MonthlyActivity { get; set; } = [];
    public List<ReportsFunnelStageViewModel> FunnelStages { get; set; } = [];
    public List<ReportsSourceViewModel> Sources { get; set; } = [];
    public List<ReportsTeamMemberViewModel> TeamMembers { get; set; } = [];
    public List<ReportsVacancyTimingViewModel> VacancyTimings { get; set; } = [];
}

public sealed class ReportsMonthlyActivityViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Applications { get; set; }
    public int Hired { get; set; }
    public bool HiredIsDemo { get; set; }
}

public sealed class ReportsFunnelStageViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsDemo { get; set; }
}

public sealed class ReportsSourceViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public bool IsDemo { get; set; }
}

public sealed class ReportsTeamMemberViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int VacancyCount { get; set; }
    public int ApplicationCount { get; set; }
    public int HiredCount { get; set; }
    public bool HiredCountIsDemo { get; set; }

    public string Initials => CompanyTeamPageViewModel.InitialsFrom(
        string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName,
        "TM");
}

public sealed class ReportsVacancyTimingViewModel
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int DaysOpen { get; set; }
    public int TimeToHireDays { get; set; }
    public bool TimeToHireIsDemo { get; set; }
}
