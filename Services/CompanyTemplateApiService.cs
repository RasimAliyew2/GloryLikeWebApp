using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class CompanyTemplateApiService : ICompanyTemplateApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CompanyTemplateApiService> _logger;

    public CompanyTemplateApiService(
        HttpClient httpClient,
        ILogger<CompanyTemplateApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<CompanyTemplateApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/company/templates?actorUserId={actorUserId}"),
            cancellationToken);
    }

    public Task<CompanyTemplateApiResult> CreateAsync(
        int actorUserId,
        SaveCompanyTemplateInput input,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            CreateSaveRequest(HttpMethod.Post, null, actorUserId, input),
            cancellationToken);
    }

    public Task<CompanyTemplateApiResult> UpdateAsync(
        int actorUserId,
        Guid templateId,
        SaveCompanyTemplateInput input,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            CreateSaveRequest(HttpMethod.Put, templateId, actorUserId, input),
            cancellationToken);
    }

    public Task<CompanyTemplateApiResult> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/company/templates/{templateId}?actorUserId={actorUserId}"),
            cancellationToken);
    }

    private static HttpRequestMessage CreateSaveRequest(
        HttpMethod method,
        Guid? templateId,
        int actorUserId,
        SaveCompanyTemplateInput input)
    {
        var request = new BackendSaveCompanyTemplateRequest
        {
            ActorUserId = actorUserId,
            Name = input.Name,
            Audience = input.Audience,
            Category = input.Category,
            Subject = input.Subject,
            Body = input.Body
        };

        return new HttpRequestMessage(
            method,
            templateId.HasValue
                ? $"api/company/templates/{templateId.Value}"
                : "api/company/templates")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
    }

    private async Task<CompanyTemplateApiResult> SendAsync(
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
                CompanyTemplateApiResponse? payload = null;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        payload = JsonSerializer.Deserialize<CompanyTemplateApiResponse>(
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
                    payload.Variables ??= [];
                    return CompanyTemplateApiResult.From(payload);
                }

                return CompanyTemplateApiResult.Fail(
                    $"Company template request failed. HTTP {(int)response.StatusCode}.");
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyTemplateApiResult.Fail(
                "Company template request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Company templates could not be synchronized with BackendApp.");
            return CompanyTemplateApiResult.Fail(
                "BackendApp could not be reached.");
        }
    }
}
