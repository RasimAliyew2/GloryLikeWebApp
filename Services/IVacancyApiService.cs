using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface IVacancyApiService
{
    Task<EmployerVacancyListApiResult> GetEmployerVacanciesAsync(
        int employerUserId,
        CancellationToken cancellationToken = default);

    Task<CreateVacancyApiResult> CreateAsync(
        int employerUserId,
        CreateVacancyInput vacancy,
        CancellationToken cancellationToken = default);
}

public sealed class EmployerVacancyListApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public EmployerVacancyListApiResponse? Data { get; private set; }

    public static EmployerVacancyListApiResult Ok(
        EmployerVacancyListApiResponse response)
    {
        return new EmployerVacancyListApiResult
        {
            Success = true,
            Message = response.Message,
            Data = response
        };
    }

    public static EmployerVacancyListApiResult Fail(string message)
    {
        return new EmployerVacancyListApiResult
        {
            Success = false,
            Message = message
        };
    }
}

public sealed class CreateVacancyApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public int? VacancyId { get; private set; }
    public string PlatformVacancyId { get; private set; } = string.Empty;

    public static CreateVacancyApiResult Ok(
        CreateVacancyApiResponse response)
    {
        return new CreateVacancyApiResult
        {
            Success = true,
            Message = response.Message,
            VacancyId = response.VacancyId,
            PlatformVacancyId = response.PlatformVacancyId
        };
    }

    public static CreateVacancyApiResult Fail(string message)
    {
        return new CreateVacancyApiResult
        {
            Success = false,
            Message = message
        };
    }
}
