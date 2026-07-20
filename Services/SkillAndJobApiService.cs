using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public sealed class SkillAndJobApiService : ISkillAndJobApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;

    public SkillAndJobApiService(HttpClient httpClient)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<SkillAndJobApiResult> GetJobFamiliesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result =
                await _httpClient.GetFromJsonAsync<List<JobFamily>>(
                    "api/SkillAndJob/job-families",
                    JsonOptions,
                    cancellationToken);

            return SkillAndJobApiResult.Ok(
                result ?? new List<JobFamily>());
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return SkillAndJobApiResult.Fail(
                "Job taxonomy sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException ex)
        {
            return SkillAndJobApiResult.Fail(
                "Job taxonomy backend-dən alınmadı: "
                + ex.Message);
        }
        catch (JsonException ex)
        {
            return SkillAndJobApiResult.Fail(
                "Job taxonomy JSON cavabı uyğun deyil: "
                + ex.Message);
        }
    }

    public async Task<SkillLookupApiResult> GetAllSkillsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result =
                await _httpClient.GetFromJsonAsync<List<SkillLookupItem>>(
                    "api/SkillAndJob/skills",
                    JsonOptions,
                    cancellationToken);

            return SkillLookupApiResult.Ok(
                result ?? new List<SkillLookupItem>());
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return SkillLookupApiResult.Fail(
                "SQL skill sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException ex)
        {
            return SkillLookupApiResult.Fail(
                "SQL skill-lər backend-dən alınmadı: "
                + ex.Message);
        }
        catch (JsonException ex)
        {
            return SkillLookupApiResult.Fail(
                "SQL skill JSON cavabı uyğun deyil: "
                + ex.Message);
        }
    }
}

public sealed class SkillAndJobApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<JobFamily> JobFamilies { get; private set; } = new();

    public static SkillAndJobApiResult Ok(
        List<JobFamily> jobFamilies)
    {
        return new SkillAndJobApiResult
        {
            Success = true,
            JobFamilies = jobFamilies
        };
    }

    public static SkillAndJobApiResult Fail(string message)
    {
        return new SkillAndJobApiResult
        {
            Success = false,
            Message = message
        };
    }
}

public sealed class SkillLookupApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<SkillLookupItem> Skills { get; private set; } = new();

    public static SkillLookupApiResult Ok(
        List<SkillLookupItem> skills)
    {
        return new SkillLookupApiResult
        {
            Success = true,
            Skills = skills
        };
    }

    public static SkillLookupApiResult Fail(string message)
    {
        return new SkillLookupApiResult
        {
            Success = false,
            Message = message
        };
    }
}
