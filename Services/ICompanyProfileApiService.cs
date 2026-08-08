using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface ICompanyProfileApiService
{
    Task<CompanyProfileApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyProfileApiResult> SaveAsync(
        int actorUserId,
        CompanyProfileInput profile,
        CancellationToken cancellationToken = default);
}

public sealed class CompanyProfileApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public CompanyProfileApiResponse? Data { get; private set; }

    public static CompanyProfileApiResult From(
        CompanyProfileApiResponse response)
    {
        return new CompanyProfileApiResult
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };
    }

    public static CompanyProfileApiResult Fail(string message)
    {
        return new CompanyProfileApiResult
        {
            Success = false,
            Message = message
        };
    }
}
