using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface ICompanyStructureApiService
{
    Task<CompanyStructureApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyStructureApiResult> SaveAsync(
        int actorUserId,
        SaveCompanyStructureInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyStructureApiResult> ImportAsync(
        int actorUserId,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<CompanyStructureFileResult> ExportAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class CompanyStructureApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public CompanyStructureApiResponse? Data { get; private set; }

    public static CompanyStructureApiResult From(CompanyStructureApiResponse response) =>
        new()
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };

    public static CompanyStructureApiResult Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}

public sealed class CompanyStructureFileResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FileName { get; init; } = "BothFind-Company-Structure.xlsx";
    public byte[] Content { get; init; } = Array.Empty<byte>();
}
