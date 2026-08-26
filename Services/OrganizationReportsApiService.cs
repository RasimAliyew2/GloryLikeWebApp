using System.Globalization;
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

    public Task<OrganizationReportsApiResult<
        OrganizationReportCatalogApiResponse>> GetCatalogAsync(
            int actorUserId,
            CancellationToken cancellationToken = default)
    {
        return GetAsync<OrganizationReportCatalogApiResponse>(
            $"api/company/reports?actorUserId={actorUserId}",
            response => response.Success,
            response => response.Message,
            "Report catalog could not be loaded.",
            cancellationToken);
    }

    public Task<OrganizationReportsApiResult<
        VacancyCreationReportApiResponse>> ExecuteVacancyCreationReportAsync(
            int actorUserId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default)
    {
        var fromValue = dateFrom.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);
        var toValue = dateTo.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        return GetAsync<VacancyCreationReportApiResponse>(
            "api/company/reports/vacancy-creation"
            + $"?actorUserId={actorUserId}"
            + $"&dateFrom={fromValue}"
            + $"&dateTo={toValue}",
            response => response.Success,
            response => response.Message,
            "Vacancy creation report could not be generated.",
            cancellationToken);
    }

    public Task<OrganizationReportsApiResult<ReportEmployeeProfileApiResponse>>
        GetEmployeeProfileAsync(
            int actorUserId,
            int employeeUserId,
            CancellationToken cancellationToken = default)
    {
        return GetAsync<ReportEmployeeProfileApiResponse>(
            $"api/company/reports/employees/{employeeUserId}"
            + $"?actorUserId={actorUserId}",
            response => response.Success,
            response => response.Message,
            "Employee profile could not be loaded.",
            cancellationToken);
    }

    private async Task<OrganizationReportsApiResult<TResponse>> GetAsync<TResponse>(
        string requestUri,
        Func<TResponse, bool> successSelector,
        Func<TResponse, string> messageSelector,
        string fallbackMessage,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                requestUri,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);
            TResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<TResponse>(
                        body,
                        JsonOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Reports API response was not valid JSON. HTTP {StatusCode}.",
                        (int)response.StatusCode);
                }
            }

            if (apiResponse is not null)
            {
                return OrganizationReportsApiResult<TResponse>.From(
                    response.IsSuccessStatusCode
                    && successSelector(apiResponse),
                    messageSelector(apiResponse),
                    apiResponse);
            }

            return OrganizationReportsApiResult<TResponse>.Fail(
                response.IsSuccessStatusCode
                    ? fallbackMessage
                    : $"{fallbackMessage} HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return OrganizationReportsApiResult<TResponse>.Fail(
                "The reports request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Reports could not be loaded from BackendApp.");

            return OrganizationReportsApiResult<TResponse>.Fail(
                "BackendApp could not be reached.");
        }
    }
}
