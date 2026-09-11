using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface ICompanyAutomationApiService
{
    Task<CompanyAutomationApiResult> RetryAsync(int actorUserId, Guid deliveryId, CancellationToken cancellationToken = default);

    Task<CompanyAutomationApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyAutomationApiResult> CreateAsync(
        int actorUserId,
        SaveCompanyAutomationInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyAutomationApiResult> UpdateAsync(
        int actorUserId,
        Guid templateId,
        SaveCompanyAutomationInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyAutomationApiResult> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default);
}

public sealed class CompanyAutomationApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public CompanyAutomationApiResponse? Data { get; private set; }

    public static CompanyAutomationApiResult From(CompanyAutomationApiResponse response) =>
        new()
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };

    public static CompanyAutomationApiResult Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}
