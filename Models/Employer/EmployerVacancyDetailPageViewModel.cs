namespace GloryLikeWebApp.Models.Employer;

public sealed class EmployerVacancyDetailPageViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public EmployerVacancyDetailViewModel? Vacancy { get; set; }

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

            return parts.Count == 0
                ? "EM"
                : string.Concat(parts.Select(
                    part => char.ToUpperInvariant(part[0])));
        }
    }
}

public sealed class EmployerVacancyDetailViewModel
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string SeniorityName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PublishDate { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int ApplicantCount { get; set; }
    public int AverageMatchScore { get; set; }
    public int HighConfidenceCount { get; set; }
    public List<EmployerVacancyApplicantViewModel> Applicants { get; set; } = new();
    public List<EmployerVacancySkillViewModel> Skills { get; set; } = new();
    public List<EmployerVacancyFunnelStageViewModel> FunnelStages { get; set; } = new();

    public EmployerVacancyApplicantViewModel? BestMatch =>
        Applicants.FirstOrDefault();

    public string Title => string.IsNullOrWhiteSpace(RoleTitle)
        ? string.IsNullOrWhiteSpace(PositionName)
            ? $"Vacancy #{VacancyId}"
            : PositionName
        : RoleTitle;

    public bool IsSuspended => Status.Equals(
        "Suspended",
        StringComparison.OrdinalIgnoreCase)
        || Status.Equals(
            "Paused",
            StringComparison.OrdinalIgnoreCase);

    public bool CanToggleStatus => IsSuspended
        || Status.Equals("Published", StringComparison.OrdinalIgnoreCase)
        || Status.Equals("Active", StringComparison.OrdinalIgnoreCase);

    public string StatusLabel => IsSuspended
        ? "Suspended"
        : Status.Equals("Published", StringComparison.OrdinalIgnoreCase)
            || Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                ? "Active"
                : string.IsNullOrWhiteSpace(Status)
                    ? "Unknown"
                    : Status;

    public string StatusCssClass => IsSuspended
        ? "suspended"
        : StatusLabel == "Active"
            ? "active"
            : "other";

    public string ToggleLabel => IsSuspended
        ? "Continue"
        : "Pause";

    public string ToggleIcon => IsSuspended
        ? "▶"
        : "Ⅱ";

    public string HiringWindowText
    {
        get
        {
            if (!ApplicationDeadline.HasValue)
                return "Open";

            var days = (ApplicationDeadline.Value.Date - DateTime.Today).Days;

            return days switch
            {
                < 0 => "Closed",
                0 => "Today",
                1 => "1 day",
                _ => $"{days} days"
            };
        }
    }
}

public sealed class EmployerVacancyApplicantViewModel
{
    public int ApplicationId { get; set; }
    public int CandidateUserId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string ApplicationStatus { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();

    public string Initials
    {
        get
        {
            var parts = CandidateName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();

            return parts.Count == 0
                ? "CA"
                : string.Concat(parts.Select(
                    part => char.ToUpperInvariant(part[0])));
        }
    }

    public string ScoreCssClass => MatchScore switch
    {
        >= 80 => "excellent",
        >= 60 => "strong",
        >= 40 => "medium",
        _ => "low"
    };

    public string ApplicationStatusLabel => ApplicationStatus.Equals(
        "NoResponseYet",
        StringComparison.OrdinalIgnoreCase)
        ? "No response yet"
        : string.IsNullOrWhiteSpace(ApplicationStatus)
            ? "Applied"
            : ApplicationStatus;

    public string AppliedText =>
        $"Applied {AppliedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}";
}

public sealed class EmployerVacancySkillViewModel
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Weight { get; set; }
    public string RequirementType { get; set; } = string.Empty;
}

public sealed class EmployerVacancyFunnelStageViewModel
{
    public string StageName { get; set; } = string.Empty;
    public int Hours { get; set; }
    public bool IsStandard { get; set; }
    public int SortOrder { get; set; }
}
