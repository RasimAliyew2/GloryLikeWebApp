using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class TalentRadarApiService : ITalentRadarApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<TalentRadarApiService> _logger;

    public TalentRadarApiService(
        HttpClient httpClient,
        ILogger<TalentRadarApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TalentRadarApiResult> GetAsync(
        int employerUserId,
        CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0)
        {
            return TalentRadarApiResult.Fail(
                "Employer user ID düzgün deyil.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/TalentRadar/{employerUserId}",
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            TalentRadarApiResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<TalentRadarApiResponse>(
                        body,
                        JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Talent Radar API cavabı JSON kimi oxunmadı. HTTP {StatusCode}.",
                        (int)response.StatusCode);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return TalentRadarApiResult.Fail(
                    ExtractMessage(
                        body,
                        apiResponse?.Message,
                        $"Talent Radar yüklənmədi. HTTP {(int)response.StatusCode}."));
            }

            if (apiResponse is null || !apiResponse.Success)
            {
                return TalentRadarApiResult.Fail(
                    apiResponse?.Message
                    ?? "Backend Talent Radar cavabı oxunmadı.");
            }

            apiResponse.Candidates ??=
                new List<TalentRadarCandidateApiItem>();

            foreach (var candidate in apiResponse.Candidates)
            {
                candidate.Skills ??=
                    new List<TalentRadarSkillApiItem>();
            }

            return TalentRadarApiResult.Ok(apiResponse);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return TalentRadarApiResult.Fail(
                "Talent Radar sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Talent Radar BackendApp-dən yüklənmədi.");

            return TalentRadarApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    private static string ExtractMessage(
        string body,
        string? responseMessage,
        string fallback)
    {
        if (!string.IsNullOrWhiteSpace(responseMessage))
            return responseMessage;

        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty(
                    "message",
                    out var message))
            {
                var value = message.GetString();

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            if (document.RootElement.TryGetProperty(
                    "title",
                    out var title))
            {
                var value = title.GetString();

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch (JsonException)
        {
            // Raw HTML/plain text istifadəçiyə göstərilmir.
        }

        return fallback;
    }
}
