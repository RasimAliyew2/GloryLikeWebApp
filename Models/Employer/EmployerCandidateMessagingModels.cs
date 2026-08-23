namespace GloryLikeWebApp.Models.Employer;

public sealed class EmployerCandidatePageViewModel
{
    public int ActorUserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public EmployerCandidateProfileApiItem? Candidate { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public string Initials => InitialsFor(DisplayName, "EM");

    public static string InitialsFor(string? value, string fallback)
    {
        var parts = (value ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .ToList();

        return parts.Count == 0
            ? fallback
            : string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
    }
}

public sealed class EmployerMessagesPageViewModel
{
    public int ActorUserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public CompanyMessagingOverviewApiResponse Overview { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public string Initials => EmployerCandidatePageViewModel.InitialsFor(DisplayName, "EM");
}

public sealed class EmployerCandidateProfileApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public EmployerCandidateProfileApiItem? Candidate { get; set; }
}

public sealed class EmployerCandidateProfileApiItem
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string About { get; set; } = string.Empty;
    public string ProfileImageDataUrl { get; set; } = string.Empty;
    public string CurrentJobName { get; set; } = string.Empty;
    public List<EmployerCandidateSkillApiItem> Skills { get; set; } = [];
    public List<EmployerCandidateExperienceApiItem> Experiences { get; set; } = [];
    public List<CandidateVacancyHistoryApiItem> VacancyHistory { get; set; } = [];
    public List<CompanyMessageTeamMemberApiItem> TeamMembers { get; set; } = [];

    public string Initials => EmployerCandidatePageViewModel.InitialsFor(DisplayName, "CA");
    public string BirthDateDisplay => BirthDate?.ToString("dd MMM yyyy") ?? "Not provided";
}

public sealed class EmployerCandidateSkillApiItem
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public int CredibilityScore { get; set; }
}

public sealed class EmployerCandidateExperienceApiItem
{
    public string CompanyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string StartYear { get; set; } = string.Empty;
    public string EndYear { get; set; } = string.Empty;
}

public sealed class CandidateVacancyHistoryApiItem
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }

    public string StatusLabel => ApplicationStatus switch
    {
        "NoResponseYet" => "Applied",
        "ScreeningPassed" => "Screening passed",
        "ScreeningFailed" => "Screening failed",
        _ => string.IsNullOrWhiteSpace(ApplicationStatus)
            ? "Applied"
            : ApplicationStatus
    };
}

public sealed class CompanyMessageTeamMemberApiItem
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Initials => EmployerCandidatePageViewModel.InitialsFor(DisplayName, "TM");
}

public sealed class CompanyMessagingOverviewApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public List<CompanyMessageTeamMemberApiItem> TeamMembers { get; set; } = [];
    public List<CompanyMessageConversationApiItem> Conversations { get; set; } = [];
}

public sealed class CompanyMessageConversationApiItem
{
    public int OtherUserId { get; set; }
    public string OtherDisplayName { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public string CandidateDisplayName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAtUtc { get; set; }
    public int UnreadCount { get; set; }
    public string Initials => EmployerCandidatePageViewModel.InitialsFor(OtherDisplayName, "TM");
}

public sealed class CompanyMessageThreadApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public List<CompanyMessageApiItem> Messages { get; set; } = [];
}

public sealed class CompanyMessageActionApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public CompanyMessageApiItem? Item { get; set; }
}

public sealed class CompanyUnreadCountApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
}

public sealed class CompanyMessageApiItem
{
    public int Id { get; set; }
    public int SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public int RecipientUserId { get; set; }
    public string RecipientDisplayName { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public string CandidateDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

public sealed class SendCompanyCandidateMessageInput
{
    public int RecipientUserId { get; set; }
    public int CandidateUserId { get; set; }
    public string Body { get; set; } = string.Empty;
}

public sealed class MarkCompanyMessageThreadReadInput
{
    public int OtherUserId { get; set; }
    public int CandidateUserId { get; set; }
}

public sealed class EmployerApiResult<T>
    where T : class
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static EmployerApiResult<T> Ok(T data) =>
        new() { Success = true, Data = data };

    public static EmployerApiResult<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
