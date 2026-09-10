using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface ICompanyFunnelApiService
{
    Task<CompanyFunnelApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyFunnelApiResult> CreateAsync(
        int actorUserId,
        SaveCompanyFunnelInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyFunnelApiResult> UpdateAsync(
        int actorUserId,
        Guid templateId,
        SaveCompanyFunnelInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyFunnelApiResult> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default);
}

public sealed class CompanyFunnelApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public CompanyFunnelApiResponse? Data { get; private set; }

    public static CompanyFunnelApiResult From(CompanyFunnelApiResponse response) =>
        new()
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };

    public static CompanyFunnelApiResult Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}
