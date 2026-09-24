namespace GloryLikeWebApp.Models.Student;

public sealed class StudentApplicationsPageViewModel
{
    public List<StudentApplicationViewItem> Applications { get; set; } = [];
    public StudentApplicationViewItem? SelectedApplication { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = "all";
    public string ErrorMessage { get; set; } = string.Empty;
    public int? HighlightVacancyId { get; set; }
    public int TotalCount { get; set; }
    public int InProgressCount { get; set; }
    public int HiredCount { get; set; }
    public int NotSelectedCount { get; set; }
    public int WithdrawnCount { get; set; }
    public bool HasFilters => SearchText.Length > 0 || StatusFilter != "all";
}

public sealed class StudentApplicationViewItem
{
    public CandidateApplicationViewItem Details { get; set; } = new();
    public string VacancyType { get; set; } = string.Empty;

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? "";
    private bool ScreeningFailed => Normalize(Details.ApplicationStatus) == "screeningfailed";

    public string StatusKey
    {
        get
        {
            var status = Normalize(Details.ApplicationStatus);
            var stage = Normalize(Details.FunnelStageName);
            if (ScreeningFailed || status is "rejected" or "declined" or "notselected"
                || stage is "rejected" or "declined" or "not selected" or "unsuccessful") return "not-selected";
            if (status is "withdrawn" or "cancelled" || stage is "withdrawn" or "cancelled") return "withdrawn";
            if (Details.HiredAtUtc.HasValue || status == "hired" || stage == "hired") return "hired";
            return "in-progress";
        }
    }

    public string StatusLabel => StatusKey switch
    {
        "not-selected" => ScreeningFailed ? "Screening not passed" : "Not selected",
        "withdrawn" => "Withdrawn",
        "hired" => "Hired",
        _ => string.IsNullOrWhiteSpace(Details.FunnelStageName) ? "Applied" : Details.FunnelStageName
    };

    public string SearchText => string.Join(" ", Details.RoleTitle, Details.CompanyName,
        Details.PlatformVacancyId, Details.LocationName, Details.JobFamilyName, StatusLabel);
}
