using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface IOrganizationReportsApiService
{
    Task<OrganizationReportsApiResult<
        OrganizationAnalyticsDashboardApiResponse>> GetDashboardAsync(
            int actorUserId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default);
}

public sealed class OrganizationReportsApiResult<TResponse>
    where TResponse : class
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public TResponse? Data { get; private set; }

    public static OrganizationReportsApiResult<TResponse> From(
        bool success,
        string message,
        TResponse response)
    {
        return new OrganizationReportsApiResult<TResponse>
        {
            Success = success,
            Message = message,
            Data = response
        };
    }

    public static OrganizationReportsApiResult<TResponse> Fail(string message)
    {
        return new OrganizationReportsApiResult<TResponse>
        {
            Success = false,
            Message = message
        };
    }
}
