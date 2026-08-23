using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface IEmployerCandidateMessagingApiService
{
    Task<EmployerApiResult<EmployerCandidateProfileApiResponse>> GetCandidateProfileAsync(
        int actorUserId,
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerApiResult<CompanyMessagingOverviewApiResponse>> GetOverviewAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerApiResult<CompanyUnreadCountApiResponse>> GetUnreadCountAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerApiResult<CompanyMessageThreadApiResponse>> GetThreadAsync(
        int actorUserId,
        int otherUserId,
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerApiResult<CompanyMessageActionApiResponse>> SendAsync(
        int actorUserId,
        SendCompanyCandidateMessageInput input,
        CancellationToken cancellationToken = default);

    Task<EmployerApiResult<CompanyMessageActionApiResponse>> MarkReadAsync(
        int actorUserId,
        MarkCompanyMessageThreadReadInput input,
        CancellationToken cancellationToken = default);
}
