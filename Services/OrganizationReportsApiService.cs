using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class OrganizationReportsApiService
    : IOrganizationReportsApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationReportsApiService> _logger;

    public OrganizationReportsApiService(
        HttpClient httpClient,
        ILogger<OrganizationReportsApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OrganizationReportsApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/company/reports?actorUserId={actorUserId}",
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);
            OrganizationReportsApiResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<
                        OrganizationReportsApiResponse>(body, JsonOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Organization reports API cavabı JSON kimi oxunmadı.");
                }
            }

            if (apiResponse is not null)
            {
                apiResponse.Categories ??= [];
                foreach (var category in apiResponse.Categories)
                    category.Metrics ??= [];

                return OrganizationReportsApiResult.From(apiResponse);
            }

            return OrganizationReportsApiResult.Fail(
                response.IsSuccessStatusCode
                    ? "Reports cavabı oxunmadı."
                    : $"Reports yüklənmədi. HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return OrganizationReportsApiResult.Fail(
                "Reports sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Organization reports BackendApp-dən yüklənmədi.");

            return OrganizationReportsApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }
}
