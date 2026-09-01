namespace GloryLikeWebApp.Models;

public sealed class CandidateApplicationsViewModel
{
    public string DisplayName { get; set; } = "Candidate";
    public string Email { get; set; } = string.Empty;
    public int? HighlightVacancyId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<CandidateApplicationViewItem> Applications { get; set; } = [];

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName)
                ? Email
                : DisplayName;
            var value = string.Concat(source
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0])));
            return string.IsNullOrWhiteSpace(value) ? "C" : value;
        }
    }
}

public sealed class CandidateApplicationDetailsViewModel
{
    public string DisplayName { get; set; } = "Candidate";
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public CandidateApplicationViewItem? Application { get; set; }

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName)
                ? Email
                : DisplayName;
            var value = string.Concat(source
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0])));
            return string.IsNullOrWhiteSpace(value) ? "C" : value;
        }
    }
}

public sealed class CandidateApplicationViewItem
{
    public int ApplicationId { get; set; }
    public int VacancyId { get; set; }
    public int CompanyOwnerUserId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string SeniorityName { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool HideSalary { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public string VacancyStatus { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = string.Empty;
    public string FunnelStageName { get; set; } = string.Empty;
    public int FunnelStageIndex { get; set; }
    public int FunnelStageCount { get; set; }
    public DateTime AppliedAtUtc { get; set; }
    public DateTime? FunnelStageUpdatedAtUtc { get; set; }
    public DateTime? HiredAtUtc { get; set; }
    public List<CandidateApplicationSkillItem> Skills { get; set; } = [];

    public int FunnelProgress => FunnelStageCount <= 0
        ? 0
        : Math.Clamp(
            (int)Math.Round(
                FunnelStageIndex * 100d / FunnelStageCount,
                MidpointRounding.AwayFromZero),
            0,
            100);

    public string CompanyInitials => string.Concat(CompanyName
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));

    public string SalaryText
    {
        get
        {
            if (HideSalary || (!MinSalary.HasValue && !MaxSalary.HasValue))
                return "Salary not disclosed";

            var currency = string.IsNullOrWhiteSpace(Currency)
                ? string.Empty
                : $" {Currency.Trim()}";

            if (MinSalary.HasValue && MaxSalary.HasValue)
                return $"{MinSalary.Value:0.##} – {MaxSalary.Value:0.##}{currency}";

            return MinSalary.HasValue
                ? $"From {MinSalary.Value:0.##}{currency}"
                : $"Up to {MaxSalary!.Value:0.##}{currency}";
        }
    }
}

public sealed class CandidateApplicationSkillItem
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Weight { get; set; }
    public string RequirementType { get; set; } = string.Empty;
}

public sealed class CandidateApplicationListApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public List<CandidateApplicationApiItem> Applications { get; set; } = [];
}

public sealed class CandidateApplicationApiItem
{
    public int ApplicationId { get; set; }
    public int VacancyId { get; set; }
    public int CompanyOwnerUserId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string SeniorityName { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool HideSalary { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public string VacancyStatus { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = string.Empty;
    public string FunnelStageName { get; set; } = string.Empty;
    public int FunnelStageIndex { get; set; }
    public int FunnelStageCount { get; set; }
    public DateTime AppliedAtUtc { get; set; }
    public DateTime? FunnelStageUpdatedAtUtc { get; set; }
    public DateTime? HiredAtUtc { get; set; }
    public List<CandidateApplicationSkillApiItem> Skills { get; set; } = [];
}

public sealed class CandidateApplicationSkillApiItem
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Weight { get; set; }
    public string RequirementType { get; set; } = string.Empty;
}

public sealed class CandidateNotificationListApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public int UnreadCount { get; set; }
    public List<CandidateNotificationApiItem> Notifications { get; set; } = [];
}

public sealed class CandidateNotificationApiItem
{
    public long NotificationId { get; set; }
    public int VacancyId { get; set; }
    public int ApplicationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

public sealed class MarkCandidateNotificationReadApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long NotificationId { get; set; }
    public int VacancyId { get; set; }
    public int ApplicationId { get; set; }
    public bool WasAlreadyRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
