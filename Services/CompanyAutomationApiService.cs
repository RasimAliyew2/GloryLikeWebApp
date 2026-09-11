using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class CompanyAutomationApiService : ICompanyAutomationApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CompanyAutomationApiService> _logger;

    public CompanyAutomationApiService(
        HttpClient httpClient,
        ILogger<CompanyAutomationApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<CompanyAutomationApiResult> RetryAsync(int actorUserId, Guid deliveryId, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post,$"api/company/automation-templates/deliveries/{deliveryId}/retry?actorUserId={actorUserId}"),cancellationToken);

    public Task<CompanyAutomationApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/company/automation-templates?actorUserId={actorUserId}"),
            cancellationToken);
    }

    public Task<CompanyAutomationApiResult> CreateAsync(
        int actorUserId,
        SaveCompanyAutomationInput input,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            CreateSaveRequest(HttpMethod.Post, null, actorUserId, input),
            cancellationToken);
    }

    public Task<CompanyAutomationApiResult> UpdateAsync(
        int actorUserId,
        Guid templateId,
        SaveCompanyAutomationInput input,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            CreateSaveRequest(HttpMethod.Put, templateId, actorUserId, input),
            cancellationToken);
    }

    public Task<CompanyAutomationApiResult> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/company/automation-templates/{templateId}?actorUserId={actorUserId}"),
            cancellationToken);
    }

    private static HttpRequestMessage CreateSaveRequest(
        HttpMethod method,
        Guid? templateId,
        int actorUserId,
        SaveCompanyAutomationInput input)
    {
        input.Rule.TargetStageName ??= string.Empty;
        var request = new BackendSaveCompanyAutomationRequest
        {
            ActorUserId = actorUserId,
            Name = input.Name,
            IsEnabled = input.IsEnabled,
            Rule = input.Rule
        };

        return new HttpRequestMessage(
            method,
            templateId.HasValue
                ? $"api/company/automation-templates/{templateId.Value}"
                : "api/company/automation-templates")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
    }

    private async Task<CompanyAutomationApiResult> SendAsync(
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
                CompanyAutomationApiResponse? payload = null;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        payload = JsonSerializer.Deserialize<CompanyAutomationApiResponse>(
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
                    return CompanyAutomationApiResult.From(payload);
                }

                return CompanyAutomationApiResult.Fail(
                    $"Company template request failed. HTTP {(int)response.StatusCode}.");
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyAutomationApiResult.Fail(
                "Company template request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Company templates could not be synchronized with BackendApp.");
            return CompanyAutomationApiResult.Fail(
                "BackendApp could not be reached.");
        }
    }
}
