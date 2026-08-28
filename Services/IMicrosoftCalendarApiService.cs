using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface IMicrosoftCalendarApiService
{
    Task<MicrosoftCalendarApiResult<MicrosoftCalendarAuthorizationUrlApiResponse>>
        GetAuthorizationUrlAsync(
            MicrosoftCalendarAuthorizationUrlApiRequest request,
            CancellationToken cancellationToken = default);

    Task<MicrosoftCalendarApiResult<MicrosoftCalendarConnectionStatusApiResponse>>
        CompleteConnectionAsync(
            CompleteMicrosoftCalendarConnectionApiRequest request,
            CancellationToken cancellationToken = default);

    Task<MicrosoftCalendarApiResult<MicrosoftCalendarConnectionStatusApiResponse>>
        GetStatusAsync(
            int employerUserId,
            CancellationToken cancellationToken = default);

    Task<MicrosoftCalendarApiResult<MicrosoftCalendarConnectionStatusApiResponse>>
        DisconnectAsync(
            int employerUserId,
            CancellationToken cancellationToken = default);

    Task<MicrosoftCalendarApiResult<CreateInterviewMeetingApiResponse>>
        CreateMeetingAsync(
            CreateInterviewMeetingApiRequest request,
            CancellationToken cancellationToken = default);
}
