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

    Task<PublicCompanyProfileApiResult> GetPublicAsync(
        int companyOwnerUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyAboutAiApiResult> CustomizeWithAiAsync(
        int actorUserId,
        CompanyAboutAiInput input,
        CancellationToken cancellationToken = default);
}

public sealed class PublicCompanyProfileApiResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public PublicCompanyProfileApiResponse? Data { get; init; }
}

public sealed class CompanyAboutAiApiResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public CompanyAboutAiApiResponse? Data { get; init; }
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
