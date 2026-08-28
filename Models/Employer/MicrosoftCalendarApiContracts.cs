namespace GloryLikeWebApp.Models.Employer;

public sealed class MicrosoftCalendarAuthorizationUrlApiRequest
{
    public int EmployerUserId { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
}

public sealed class MicrosoftCalendarAuthorizationUrlApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AuthorizationUrl { get; set; } = string.Empty;
}

public sealed class CompleteMicrosoftCalendarConnectionApiRequest
{
    public int EmployerUserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

public sealed class MicrosoftCalendarConnectionStatusApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public bool IsConnected { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime? ConnectedAtUtc { get; set; }
}

public sealed class CreateInterviewMeetingApiRequest
{
    public int EmployerUserId { get; set; }
    public int VacancyId { get; set; }
    public int ApplicationId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Agenda { get; set; } = string.Empty;
    public DateTimeOffset StartAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public bool CreateTeamsMeeting { get; set; }
}

public sealed class CreateInterviewMeetingApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MeetingId { get; set; }
    public string CandidateEmail { get; set; } = string.Empty;
    public string OrganizerEmail { get; set; } = string.Empty;
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public string WebLink { get; set; } = string.Empty;
    public string JoinUrl { get; set; } = string.Empty;
}

public sealed class MicrosoftCalendarApiResult<T>
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public T? Data { get; private set; }

    public static MicrosoftCalendarApiResult<T> Ok(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    public static MicrosoftCalendarApiResult<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
