using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class CompanyHiringPlanApiService : ICompanyHiringPlanApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CompanyHiringPlanApiService> _logger;

    public CompanyHiringPlanApiService(
        HttpClient httpClient,
        ILogger<CompanyHiringPlanApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<CompanyHiringPlanApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/company/hiring-plan?actorUserId={actorUserId}"),
            cancellationToken);
    }

    public Task<CompanyHiringPlanApiResult> GetByIdAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/company/hiring-plan/{planId}?actorUserId={actorUserId}"),
            cancellationToken);
    }

    public Task<CompanyHiringPlanApiResult> CreateAsync(
        int actorUserId,
        SaveCompanyHiringPlanInput input,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            CreateSaveRequest(HttpMethod.Post, null, actorUserId, input),
            cancellationToken);
    }

    public Task<CompanyHiringPlanApiResult> UpdateAsync(
        int actorUserId,
        int planId,
        SaveCompanyHiringPlanInput input,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            CreateSaveRequest(HttpMethod.Put, planId, actorUserId, input),
            cancellationToken);
    }

    public Task<CompanyHiringPlanApiResult> DeleteAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/company/hiring-plan/{planId}?actorUserId={actorUserId}"),
            cancellationToken);
    }

    private static HttpRequestMessage CreateSaveRequest(
        HttpMethod method,
        int? planId,
        int actorUserId,
        SaveCompanyHiringPlanInput input)
    {
        var request = new BackendSaveCompanyHiringPlanRequest
        {
            ActorUserId = actorUserId,
            JobFamilyId = input.JobFamilyId,
            PositionId = input.PositionId,
            SeniorityId = input.SeniorityId,
            Headcount = input.Headcount,
            Priority = input.Priority,
            TargetStartDate = input.TargetStartDate,
            EmploymentType = input.EmploymentType,
            Notes = input.Notes
        };

        return new HttpRequestMessage(
            method,
            planId.HasValue
                ? $"api/company/hiring-plan/{planId.Value}"
                : "api/company/hiring-plan")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
    }

    private async Task<CompanyHiringPlanApiResult> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using (request)
            using (var response = await _httpClient.SendAsync(request, cancellationToken))
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                CompanyHiringPlanApiResponse? payload = null;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        payload = JsonSerializer.Deserialize<CompanyHiringPlanApiResponse>(
                            body,
                            JsonOptions);
                    }
                    catch (JsonException exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Hiring plan API response was not valid JSON.");
                    }
                }

                if (payload is not null
                    && (payload.Success || !string.IsNullOrWhiteSpace(payload.Message)))
                {
                    payload.Plans ??= new();
                    return CompanyHiringPlanApiResult.From(payload);
                }

                return CompanyHiringPlanApiResult.Fail(
                    ExtractValidationMessage(body)
                    ?? $"Hiring plan request failed. HTTP {(int)response.StatusCode}.");
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyHiringPlanApiResult.Fail("Hiring plan request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Hiring plan could not be synchronized with BackendApp.");
            return CompanyHiringPlanApiResult.Fail("BackendApp could not be reached.");
        }
    }

    private static string? ExtractValidationMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("errors", out var errors))
                return null;

            foreach (var property in errors.EnumerateObject())
            {
                var message = property.Value.EnumerateArray()
                    .Select(item => item.GetString())
                    .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
