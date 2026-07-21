using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface IVacancyApiService
{
    Task<CandidateVacancyListApiResult> GetCandidateVacanciesAsync(
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerVacancyListApiResult> GetEmployerVacanciesAsync(
        int employerUserId,
        CancellationToken cancellationToken = default);

    Task<ApplyToVacancyApiResult> ApplyAsync(
        int vacancyId,
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<CreateVacancyApiResult> CreateAsync(
        int employerUserId,
        CreateVacancyInput vacancy,
        CancellationToken cancellationToken = default);
}

public sealed class CandidateVacancyListApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public CandidateVacancyListApiResponse? Data { get; private set; }

    public static CandidateVacancyListApiResult Ok(
        CandidateVacancyListApiResponse response)
    {
        return new CandidateVacancyListApiResult
        {
            Success = true,
            Message = response.Message,
            Data = response
        };
    }

    public static CandidateVacancyListApiResult Fail(string message)
    {
        return new CandidateVacancyListApiResult
        {
            Success = false,
            Message = message
        };
    }
}

public sealed class ApplyToVacancyApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public ApplyToVacancyApiResponse? Data { get; private set; }

    public static ApplyToVacancyApiResult Ok(
        ApplyToVacancyApiResponse response)
    {
        return new ApplyToVacancyApiResult
        {
            Success = true,
            Message = response.Message,
            Data = response
        };
    }

    public static ApplyToVacancyApiResult Fail(string message)
    {
        return new ApplyToVacancyApiResult
        {
            Success = false,
            Message = message
        };
    }
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
