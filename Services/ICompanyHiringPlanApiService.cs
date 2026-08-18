using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface ICompanyHiringPlanApiService
{
    Task<CompanyHiringPlanApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanApiResult> GetByIdAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanApiResult> CreateAsync(
        int actorUserId,
        SaveCompanyHiringPlanInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanApiResult> UpdateAsync(
        int actorUserId,
        int planId,
        SaveCompanyHiringPlanInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanApiResult> DeleteAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default);
}

public sealed class CompanyHiringPlanApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public CompanyHiringPlanApiResponse? Data { get; private set; }

    public static CompanyHiringPlanApiResult From(CompanyHiringPlanApiResponse response) =>
        new()
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };

    public static CompanyHiringPlanApiResult Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}
