namespace GloryLikeWebApp.Models.Employer;

public sealed class OrganizationReportsPageViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime? GeneratedAtUtc { get; set; }
    public List<OrganizationReportCategoryViewModel> Categories { get; set; } = [];

    public string Initials => CompanyTeamPageViewModel.InitialsFrom(
        string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName,
        "EM");
}

public sealed class OrganizationReportsApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public List<OrganizationReportCategoryViewModel> Categories { get; set; } = [];
}

public sealed class OrganizationReportCategoryViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<OrganizationReportMetricViewModel> Metrics { get; set; } = [];
}

public sealed class OrganizationReportMetricViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Tone { get; set; } = "neutral";
}
