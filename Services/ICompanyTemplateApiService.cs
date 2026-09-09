using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface ICompanyTemplateApiService
{
    Task<CompanyTemplateApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyTemplateApiResult> CreateAsync(
        int actorUserId,
        SaveCompanyTemplateInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyTemplateApiResult> UpdateAsync(
        int actorUserId,
        Guid templateId,
        SaveCompanyTemplateInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyTemplateApiResult> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default);
}

public sealed class CompanyTemplateApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public CompanyTemplateApiResponse? Data { get; private set; }

    public static CompanyTemplateApiResult From(CompanyTemplateApiResponse response) =>
        new()
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };

    public static CompanyTemplateApiResult Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}
