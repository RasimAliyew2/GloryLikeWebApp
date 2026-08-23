using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class EmployerCandidateMessagingApiService
    : IEmployerCandidateMessagingApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<EmployerCandidateMessagingApiService> _logger;

    public EmployerCandidateMessagingApiService(
        HttpClient httpClient,
        ILogger<EmployerCandidateMessagingApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmployerApiResult<EmployerCandidateProfileApiResponse>>
        GetCandidateProfileAsync(
            int actorUserId,
            int candidateUserId,
            CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            $"api/employer/candidates/{candidateUserId}?actorUserId={actorUserId}",
            (EmployerCandidateProfileApiResponse item) => item.Success,
            item => item.Message,
            "Candidate profili yüklənmədi.",
            cancellationToken);
    }

    public async Task<EmployerApiResult<CompanyMessagingOverviewApiResponse>>
        GetOverviewAsync(
            int actorUserId,
            CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            $"api/company/messages/overview?actorUserId={actorUserId}",
            (CompanyMessagingOverviewApiResponse item) => item.Success,
            item => item.Message,
            "Company mesajları yüklənmədi.",
            cancellationToken);
    }

    public async Task<EmployerApiResult<CompanyUnreadCountApiResponse>>
        GetUnreadCountAsync(
            int actorUserId,
            CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            $"api/company/messages/unread-count?actorUserId={actorUserId}",
            (CompanyUnreadCountApiResponse item) => item.Success,
            item => item.Message,
            "Unread message sayı yüklənmədi.",
            cancellationToken);
    }

    public async Task<EmployerApiResult<CompanyMessageThreadApiResponse>>
        GetThreadAsync(
            int actorUserId,
            int otherUserId,
            int candidateUserId,
            CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            $"api/company/messages/thread?actorUserId={actorUserId}&otherUserId={otherUserId}&candidateUserId={candidateUserId}",
            (CompanyMessageThreadApiResponse item) => item.Success,
            item => item.Message,
            "Conversation yüklənmədi.",
            cancellationToken);
    }

    public async Task<EmployerApiResult<CompanyMessageActionApiResponse>> SendAsync(
        int actorUserId,
        SendCompanyCandidateMessageInput input,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            ActorUserId = actorUserId,
            input.RecipientUserId,
            input.CandidateUserId,
            input.Body
        };

        return await PostAsync(
            "api/company/messages",
            payload,
            (CompanyMessageActionApiResponse item) => item.Success,
            item => item.Message,
            "Mesaj göndərilmədi.",
            cancellationToken);
    }

    public async Task<EmployerApiResult<CompanyMessageActionApiResponse>> MarkReadAsync(
        int actorUserId,
        MarkCompanyMessageThreadReadInput input,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            ActorUserId = actorUserId,
            input.OtherUserId,
            input.CandidateUserId
        };

        return await PostAsync(
            "api/company/messages/read",
            payload,
            (CompanyMessageActionApiResponse item) => item.Success,
            item => item.Message,
            "Conversation oxunmuş kimi qeyd edilmədi.",
            cancellationToken);
    }

    private async Task<EmployerApiResult<T>> GetAsync<T>(
        string url,
        Func<T, bool> isSuccess,
        Func<T, string> getMessage,
        string fallback,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            return await ReadAsync(response, isSuccess, getMessage, fallback, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return EmployerApiResult<T>.Fail("Backend sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Employer candidate messaging backend request failed.");
            return EmployerApiResult<T>.Fail("Backend-ə qoşulmaq mümkün olmadı.");
        }
    }

    private async Task<EmployerApiResult<T>> PostAsync<T, TPayload>(
        string url,
        TPayload payload,
        Func<T, bool> isSuccess,
        Func<T, string> getMessage,
        string fallback,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                url,
                payload,
                JsonOptions,
                cancellationToken);
            return await ReadAsync(response, isSuccess, getMessage, fallback, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return EmployerApiResult<T>.Fail("Backend sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Employer candidate messaging backend request failed.");
            return EmployerApiResult<T>.Fail("Backend-ə qoşulmaq mümkün olmadı.");
        }
    }

    private static async Task<EmployerApiResult<T>> ReadAsync<T>(
        HttpResponseMessage response,
        Func<T, bool> isSuccess,
        Func<T, string> getMessage,
        string fallback,
        CancellationToken cancellationToken)
        where T : class
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        T? data = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                data = JsonSerializer.Deserialize<T>(body, JsonOptions);
            }
            catch (JsonException)
            {
                // HTML error pages are deliberately not forwarded to the browser.
            }
        }

        if (!response.IsSuccessStatusCode || data is null || !isSuccess(data))
        {
            var message = data is null ? string.Empty : getMessage(data);
            return EmployerApiResult<T>.Fail(
                string.IsNullOrWhiteSpace(message)
                    ? $"{fallback} HTTP {(int)response.StatusCode}."
                    : message);
        }

        return EmployerApiResult<T>.Ok(data);
    }
}
