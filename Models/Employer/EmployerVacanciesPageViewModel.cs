namespace GloryLikeWebApp.Models.Employer;

public sealed class EmployerVacanciesPageViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public List<EmployerVacancyListItemViewModel> Vacancies { get; set; } = new();

    public int AllCount => Vacancies.Count;
    public int ActiveCount => Vacancies.Count(item => item.StatusKey == "active");
    public int SuspendedCount => Vacancies.Count(item => item.StatusKey == "suspended");
    public int DraftCount => Vacancies.Count(item => item.StatusKey == "draft");

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

public sealed class EmployerVacancyListItemViewModel
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public string VacancyName => string.IsNullOrWhiteSpace(RoleTitle)
        ? string.IsNullOrWhiteSpace(PositionName)
            ? $"Vacancy #{VacancyId}"
            : PositionName
        : RoleTitle;

    public string Direction => string.IsNullOrWhiteSpace(JobFamilyName)
        ? PositionName
        : JobFamilyName;

    public string StatusKey
    {
        get
        {
            return (Status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "published" or "active" => "active",
                "suspended" or "paused" => "suspended",
                "draft" => "draft",
                _ => "other"
            };
        }
    }

    public string StatusLabel
    {
        get
        {
            return StatusKey switch
            {
                "active" => "Active",
                "suspended" => "Suspended",
                "draft" => "Draft",
                _ => string.IsNullOrWhiteSpace(Status) ? "Unknown" : Status.Trim()
            };
        }
    }

    public string StatusCssClass => $"status-{StatusKey}";

    public string DateText => (PublishDate ?? CreatedAtUtc)
        .ToString("dd.MM.yyyy");

    public string DateIso => (PublishDate ?? CreatedAtUtc).ToString("O");

    public string SearchValue => string.Join(
        " ",
        new[]
        {
            VacancyName,
            Direction,
            PositionName,
            PlatformVacancyId,
            StatusLabel
        }).ToLowerInvariant();
}
