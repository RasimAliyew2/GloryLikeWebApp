using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public sealed class SkillAssessmentApiService : ISkillAssessmentApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;

    public SkillAssessmentApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<SkillAssessmentApiResult<SkillQuestionnaireResponse>>
        GenerateAsync(
            GenerateSkillQuestionnaireRequest request,
            CancellationToken cancellationToken = default)
    {
        return PostAsync<
            GenerateSkillQuestionnaireRequest,
            SkillQuestionnaireResponse>(
            "api/SkillQuestionnaires/generate",
            request,
            "AI skill sualları yaradıla bilmədi.",
            cancellationToken);
    }

    public Task<SkillAssessmentApiResult<SkillDepthAssessmentResult>>
        SubmitAsync(
            SubmitSkillDepthAssessmentRequest request,
            CancellationToken cancellationToken = default)
    {
        return PostAsync<
            SubmitSkillDepthAssessmentRequest,
            SkillDepthAssessmentResult>(
            "api/SkillDepthAssessments/submit",
            request,
            "Skill test nəticəsi hesablana bilmədi.",
            cancellationToken);
    }

    private async Task<SkillAssessmentApiResult<TResponse>> PostAsync<
        TRequest,
        TResponse>(
        string url,
        TRequest request,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                url,
                request,
                JsonOptions,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return SkillAssessmentApiResult<TResponse>.Fail(
                    ExtractMessage(body, fallbackMessage));
            }

            var data = JsonSerializer.Deserialize<TResponse>(
                body,
                JsonOptions);

            return data is null
                ? SkillAssessmentApiResult<TResponse>.Fail(
                    "Backend assessment cavabı oxunmadı.")
                : SkillAssessmentApiResult<TResponse>.Ok(data);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return SkillAssessmentApiResult<TResponse>.Fail(
                "AI assessment sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException ex)
        {
            return SkillAssessmentApiResult<TResponse>.Fail(
                "Backend-ə qoşulmaq olmadı: " + ex.Message);
        }
        catch (JsonException ex)
        {
            return SkillAssessmentApiResult<TResponse>.Fail(
                "Assessment JSON cavabı uyğun deyil: " + ex.Message);
        }
    }

    private static string ExtractMessage(
        string? body,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        var trimmed = body.Trim();
        if (trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            return fallback + " Backend HTML xəta səhifəsi qaytardı.";
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var message))
                return message.GetString() ?? fallback;

            if (root.TryGetProperty("title", out var title))
                return title.GetString() ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }

        return fallback;
    }
}

public sealed class SkillAssessmentApiResult<T>
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public T? Data { get; private set; }

    public static SkillAssessmentApiResult<T> Ok(T data)
    {
        return new SkillAssessmentApiResult<T>
        {
            Success = true,
            Data = data
        };
    }

    public static SkillAssessmentApiResult<T> Fail(string message)
    {
        return new SkillAssessmentApiResult<T>
        {
            Success = false,
            Message = message
        };
    }
}
