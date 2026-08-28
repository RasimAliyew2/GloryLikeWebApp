using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class MicrosoftCalendarApiService : IMicrosoftCalendarApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<MicrosoftCalendarApiService> _logger;

    public MicrosoftCalendarApiService(
        HttpClient httpClient,
        ILogger<MicrosoftCalendarApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<MicrosoftCalendarApiResult<MicrosoftCalendarAuthorizationUrlApiResponse>>
        GetAuthorizationUrlAsync(
            MicrosoftCalendarAuthorizationUrlApiRequest request,
            CancellationToken cancellationToken = default) =>
        PostAsync<MicrosoftCalendarAuthorizationUrlApiRequest,
            MicrosoftCalendarAuthorizationUrlApiResponse>(
            "api/microsoft-calendar/authorization-url",
            request,
            cancellationToken);

    public Task<MicrosoftCalendarApiResult<MicrosoftCalendarConnectionStatusApiResponse>>
        CompleteConnectionAsync(
            CompleteMicrosoftCalendarConnectionApiRequest request,
            CancellationToken cancellationToken = default) =>
        PostAsync<CompleteMicrosoftCalendarConnectionApiRequest,
            MicrosoftCalendarConnectionStatusApiResponse>(
            "api/microsoft-calendar/complete",
            request,
            cancellationToken);

    public async Task<MicrosoftCalendarApiResult<MicrosoftCalendarConnectionStatusApiResponse>>
        GetStatusAsync(
            int employerUserId,
            CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/microsoft-calendar/status/{employerUserId}",
                cancellationToken);
            return await ReadResultAsync<MicrosoftCalendarConnectionStatusApiResponse>(
                response,
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "Microsoft Calendar status API çağırışı uğursuz oldu.");
            return MicrosoftCalendarApiResult<MicrosoftCalendarConnectionStatusApiResponse>
                .Fail("BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public async Task<MicrosoftCalendarApiResult<MicrosoftCalendarConnectionStatusApiResponse>>
        DisconnectAsync(
            int employerUserId,
            CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.DeleteAsync(
                $"api/microsoft-calendar/connection/{employerUserId}",
                cancellationToken);
            return await ReadResultAsync<MicrosoftCalendarConnectionStatusApiResponse>(
                response,
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "Microsoft Calendar disconnect API çağırışı uğursuz oldu.");
            return MicrosoftCalendarApiResult<MicrosoftCalendarConnectionStatusApiResponse>
                .Fail("BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public Task<MicrosoftCalendarApiResult<CreateInterviewMeetingApiResponse>>
        CreateMeetingAsync(
            CreateInterviewMeetingApiRequest request,
            CancellationToken cancellationToken = default) =>
        PostAsync<CreateInterviewMeetingApiRequest,
            CreateInterviewMeetingApiResponse>(
            "api/microsoft-calendar/meetings",
            request,
            cancellationToken);

    private async Task<MicrosoftCalendarApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                url,
                request,
                JsonOptions,
                cancellationToken);
            return await ReadResultAsync<TResponse>(response, cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "Microsoft Calendar API {Url} çağırışı uğursuz oldu.", url);
            return MicrosoftCalendarApiResult<TResponse>
                .Fail("BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    private static async Task<MicrosoftCalendarApiResult<T>> ReadResultAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        T? payload = default;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                payload = JsonSerializer.Deserialize<T>(body, JsonOptions);
            }
            catch (JsonException)
            {
                // The fallback below returns a safe HTTP error.
            }
        }

        var message = ExtractMessage(body)
            ?? $"Microsoft Calendar API HTTP {(int)response.StatusCode}.";
        if (!response.IsSuccessStatusCode || payload is null)
            return MicrosoftCalendarApiResult<T>.Fail(message);

        return MicrosoftCalendarApiResult<T>.Ok(payload, message);
    }

    private static string? ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString();
        }
        catch (JsonException)
        {
            // Ignore non-JSON backend responses.
        }
        return null;
    }
}
