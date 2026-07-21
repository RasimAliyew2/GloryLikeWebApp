using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class VacancyApiService : IVacancyApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<VacancyApiService> _logger;

    public VacancyApiService(
        HttpClient httpClient,
        ILogger<VacancyApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmployerVacancyListApiResult>
        GetEmployerVacanciesAsync(
            int employerUserId,
            CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0)
        {
            return EmployerVacancyListApiResult.Fail(
                "Employer user ID düzgün deyil.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/Vacancies/employer/{employerUserId}",
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            EmployerVacancyListApiResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<EmployerVacancyListApiResponse>(
                        body,
                        JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Employer vacancies API cavabı JSON kimi oxunmadı. HTTP {StatusCode}.",
                        (int)response.StatusCode);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return EmployerVacancyListApiResult.Fail(
                    ExtractMessage(
                        body,
                        apiResponse?.Message,
                        $"Vacancies yüklənmədi. HTTP {(int)response.StatusCode}."));
            }

            if (apiResponse is null || !apiResponse.Success)
            {
                return EmployerVacancyListApiResult.Fail(
                    apiResponse?.Message
                    ?? "Backend vacancy list cavabı oxunmadı.");
            }

            apiResponse.Vacancies ??= new List<EmployerVacancyListApiItem>();

            return EmployerVacancyListApiResult.Ok(apiResponse);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return EmployerVacancyListApiResult.Fail(
                "Vacancy list sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Vacancies BackendApp-dən yüklənmədi.");

            return EmployerVacancyListApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public async Task<CreateVacancyApiResult> CreateAsync(
        int employerUserId,
        CreateVacancyInput vacancy,
        CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0)
        {
            return CreateVacancyApiResult.Fail(
                "Employer user ID düzgün deyil.");
        }

        var request = new CreateVacancyApiRequest
        {
            EmployerUserId = employerUserId,
            Vacancy = vacancy
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/Vacancies",
                request,
                JsonOptions,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            CreateVacancyApiResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<CreateVacancyApiResponse>(
                        body,
                        JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Vacancies API cavabı JSON kimi oxunmadı. HTTP {StatusCode}.",
                        (int)response.StatusCode);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return CreateVacancyApiResult.Fail(
                    ExtractMessage(
                        body,
                        apiResponse?.Message,
                        $"Vacancy SQL-də yaradılmadı. HTTP {(int)response.StatusCode}."));
            }

            if (apiResponse is null || !apiResponse.Success)
            {
                return CreateVacancyApiResult.Fail(
                    apiResponse?.Message
                    ?? "Backend vacancy cavabı oxunmadı.");
            }

            return CreateVacancyApiResult.Ok(apiResponse);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CreateVacancyApiResult.Fail(
                "Vacancy save sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Vacancy BackendApp-ə göndərilmədi.");

            return CreateVacancyApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı. BackendApp-in işlədiyini yoxlayın.");
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
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var message))
            {
                var value = message.GetString();

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            if (root.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    var firstError = property.Value
                        .EnumerateArray()
                        .Select(item => item.GetString())
                        .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

                    if (!string.IsNullOrWhiteSpace(firstError))
                        return firstError;
                }
            }

            if (root.TryGetProperty("title", out var title))
            {
                var value = title.GetString();

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch (JsonException)
        {
            // Backend plain text/HTML qaytararsa istifadəçiyə raw body göstərilmir.
        }

        return fallback;
    }
}
