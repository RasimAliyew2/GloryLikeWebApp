using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class CompanyFunnelApiService : ICompanyFunnelApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CompanyFunnelApiService> _logger;

    public CompanyFunnelApiService(
        HttpClient httpClient,
        ILogger<CompanyFunnelApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<CompanyFunnelApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/company/funnel-templates?actorUserId={actorUserId}"),
            cancellationToken);
    }

    public Task<CompanyFunnelApiResult> CreateAsync(
        int actorUserId,
        SaveCompanyFunnelInput input,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            CreateSaveRequest(HttpMethod.Post, null, actorUserId, input),
            cancellationToken);
    }

    public Task<CompanyFunnelApiResult> UpdateAsync(
        int actorUserId,
        Guid templateId,
        SaveCompanyFunnelInput input,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            CreateSaveRequest(HttpMethod.Put, templateId, actorUserId, input),
            cancellationToken);
    }

    public Task<CompanyFunnelApiResult> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/company/funnel-templates/{templateId}?actorUserId={actorUserId}"),
            cancellationToken);
    }

    private static HttpRequestMessage CreateSaveRequest(
        HttpMethod method,
        Guid? templateId,
        int actorUserId,
        SaveCompanyFunnelInput input)
    {
        var request = new BackendSaveCompanyFunnelRequest
        {
            ActorUserId = actorUserId,
            Name = input.Name,
            Description = input.Description,
            Stages = input.Stages
        };

        return new HttpRequestMessage(
            method,
            templateId.HasValue
                ? $"api/company/funnel-templates/{templateId.Value}"
                : "api/company/funnel-templates")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
    }

    private async Task<CompanyFunnelApiResult> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using (request)
            using (var response = await _httpClient.SendAsync(
                       request,
                       cancellationToken))
            {
                var body = await response.Content.ReadAsStringAsync(
                    cancellationToken);
                CompanyFunnelApiResponse? payload = null;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        payload = JsonSerializer.Deserialize<CompanyFunnelApiResponse>(
                            body,
                            JsonOptions);
                    }
                    catch (JsonException exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Company templates API response was not valid JSON.");
                    }
                }

                if (payload is not null)
                {
                    payload.Templates ??= [];
                    return CompanyFunnelApiResult.From(payload);
                }

                return CompanyFunnelApiResult.Fail(
                    $"Company template request failed. HTTP {(int)response.StatusCode}.");
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyFunnelApiResult.Fail(
                "Company template request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Company templates could not be synchronized with BackendApp.");
            return CompanyFunnelApiResult.Fail(
                "BackendApp could not be reached.");
        }
    }
}
