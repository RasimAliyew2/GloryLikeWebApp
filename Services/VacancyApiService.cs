using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models;
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

    public async Task<CandidateVacancyListApiResult>
        GetCandidateVacanciesAsync(
            int candidateUserId,
            CancellationToken cancellationToken = default)
    {
        if (candidateUserId <= 0)
        {
            return CandidateVacancyListApiResult.Fail(
                "Candidate user ID düzgün deyil.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/Vacancies/candidate/{candidateUserId}",
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);
            CandidateVacancyListApiResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<CandidateVacancyListApiResponse>(
                        body,
                        JsonOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Candidate vacancies API cavabı JSON kimi oxunmadı. HTTP {StatusCode}.",
                        (int)response.StatusCode);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return CandidateVacancyListApiResult.Fail(
                    ExtractMessage(
                        body,
                        apiResponse?.Message,
                        $"Candidate vacancies yüklənmədi. HTTP {(int)response.StatusCode}."));
            }

            if (apiResponse is null || !apiResponse.Success)
            {
                return CandidateVacancyListApiResult.Fail(
                    apiResponse?.Message
                    ?? "Backend candidate vacancy cavabı oxunmadı.");
            }

            apiResponse.CandidateJobFamilyIds ??= new List<int>();
            apiResponse.CandidateJobFamilyNames ??= new List<string>();
            apiResponse.Vacancies ??= new List<CandidateVacancyApiItem>();

            foreach (var vacancy in apiResponse.Vacancies)
                vacancy.Skills ??= new List<CandidateVacancySkillApiItem>();

            return CandidateVacancyListApiResult.Ok(apiResponse);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CandidateVacancyListApiResult.Fail(
                "Candidate vacancy sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Candidate vacancies BackendApp-dən yüklənmədi.");

            return CandidateVacancyListApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
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

    public async Task<EmployerVacancyDetailApiResult>
        GetEmployerVacancyDetailAsync(
            int employerUserId,
            int vacancyId,
            CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0 || vacancyId <= 0)
        {
            return EmployerVacancyDetailApiResult.Fail(
                "Employer və vacancy ID düzgün deyil.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/Vacancies/employer/{employerUserId}/{vacancyId}",
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);
            EmployerVacancyDetailApiResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<EmployerVacancyDetailApiResponse>(
                        body,
                        JsonOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Employer vacancy detail API cavabı JSON kimi oxunmadı. HTTP {StatusCode}.",
                        (int)response.StatusCode);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return EmployerVacancyDetailApiResult.Fail(
                    ExtractMessage(
                        body,
                        apiResponse?.Message,
                        $"Vacancy detail yüklənmədi. HTTP {(int)response.StatusCode}."));
            }

            if (apiResponse is null
                || !apiResponse.Success
                || apiResponse.Vacancy is null)
            {
                return EmployerVacancyDetailApiResult.Fail(
                    apiResponse?.Message
                    ?? "Backend vacancy detail cavabı oxunmadı.");
            }

            apiResponse.Vacancy.Applicants ??=
                new List<EmployerVacancyApplicantApiItem>();
            apiResponse.Vacancy.Skills ??=
                new List<EmployerVacancySkillApiItem>();
            apiResponse.Vacancy.FunnelStages ??=
                new List<EmployerVacancyFunnelStageApiItem>();

            foreach (var applicant in apiResponse.Vacancy.Applicants)
            {
                applicant.MatchedSkills ??= new List<string>();
                applicant.MissingSkills ??= new List<string>();
            }

            if (apiResponse.Vacancy.BestMatch is not null)
            {
                apiResponse.Vacancy.BestMatch.MatchedSkills ??=
                    new List<string>();
                apiResponse.Vacancy.BestMatch.MissingSkills ??=
                    new List<string>();
            }

            return EmployerVacancyDetailApiResult.Ok(apiResponse);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return EmployerVacancyDetailApiResult.Fail(
                "Vacancy detail sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Employer vacancy detail BackendApp-dən yüklənmədi.");

            return EmployerVacancyDetailApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public async Task<ToggleEmployerVacancyStatusApiResult>
        ToggleEmployerStatusAsync(
            int employerUserId,
            int vacancyId,
            CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0 || vacancyId <= 0)
        {
            return ToggleEmployerVacancyStatusApiResult.Fail(
                "Employer və vacancy ID düzgün deyil.");
        }

        var request = new ToggleEmployerVacancyStatusApiRequest
        {
            EmployerUserId = employerUserId
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"api/Vacancies/{vacancyId}/employer-status/toggle",
                request,
                JsonOptions,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);
            ToggleEmployerVacancyStatusApiResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<ToggleEmployerVacancyStatusApiResponse>(
                        body,
                        JsonOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Vacancy status API cavabı JSON kimi oxunmadı. HTTP {StatusCode}.",
                        (int)response.StatusCode);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return ToggleEmployerVacancyStatusApiResult.Fail(
                    ExtractMessage(
                        body,
                        apiResponse?.Message,
                        $"Vacancy statusu dəyişmədi. HTTP {(int)response.StatusCode}."));
            }

            if (apiResponse is null || !apiResponse.Success)
            {
                return ToggleEmployerVacancyStatusApiResult.Fail(
                    apiResponse?.Message
                    ?? "Backend vacancy status cavabı oxunmadı.");
            }

            return ToggleEmployerVacancyStatusApiResult.Ok(apiResponse);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ToggleEmployerVacancyStatusApiResult.Fail(
                "Vacancy status sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Vacancy statusu BackendApp-də dəyişdirilmədi.");

            return ToggleEmployerVacancyStatusApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public async Task<ApplyToVacancyApiResult> ApplyAsync(
        int vacancyId,
        int candidateUserId,
        CancellationToken cancellationToken = default)
    {
        if (vacancyId <= 0 || candidateUserId <= 0)
        {
            return ApplyToVacancyApiResult.Fail(
                "Vacancy və candidate user ID düzgün deyil.");
        }

        var request = new ApplyToVacancyApiRequest
        {
            CandidateUserId = candidateUserId
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"api/Vacancies/{vacancyId}/applications",
                request,
                JsonOptions,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);
            ApplyToVacancyApiResponse? apiResponse = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    apiResponse = JsonSerializer.Deserialize<ApplyToVacancyApiResponse>(
                        body,
                        JsonOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Apply API cavabı JSON kimi oxunmadı. HTTP {StatusCode}.",
                        (int)response.StatusCode);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return ApplyToVacancyApiResult.Fail(
                    ExtractMessage(
                        body,
                        apiResponse?.Message,
                        $"Müraciət SQL-də saxlanmadı. HTTP {(int)response.StatusCode}."));
            }

            if (apiResponse is null || !apiResponse.Success)
            {
                return ApplyToVacancyApiResult.Fail(
                    apiResponse?.Message
                    ?? "Backend apply cavabı oxunmadı.");
            }

            return ApplyToVacancyApiResult.Ok(apiResponse);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ApplyToVacancyApiResult.Fail(
                "Apply sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Candidate vacancy apply BackendApp-ə göndərilmədi.");

            return ApplyToVacancyApiResult.Fail(
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
