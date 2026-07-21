using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface ITalentRadarApiService
{
    Task<TalentRadarApiResult> GetAsync(
        int employerUserId,
        CancellationToken cancellationToken = default);
}

public sealed class TalentRadarApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public TalentRadarApiResponse? Data { get; private set; }

    public static TalentRadarApiResult Ok(
        TalentRadarApiResponse data)
    {
        return new TalentRadarApiResult
        {
            Success = true,
            Message = data.Message,
            Data = data
        };
    }

    public static TalentRadarApiResult Fail(string message)
    {
        return new TalentRadarApiResult
        {
            Success = false,
            Message = message
        };
    }
}
