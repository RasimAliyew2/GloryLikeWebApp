using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface IOrganizationReportsApiService
{
    Task<OrganizationReportsApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class OrganizationReportsApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public OrganizationReportsApiResponse? Data { get; private set; }

    public static OrganizationReportsApiResult From(
        OrganizationReportsApiResponse response)
    {
        return new OrganizationReportsApiResult
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };
    }

    public static OrganizationReportsApiResult Fail(string message)
    {
        return new OrganizationReportsApiResult
        {
            Success = false,
            Message = message
        };
    }
}
