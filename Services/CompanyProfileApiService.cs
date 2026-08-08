using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class CompanyProfileApiService : ICompanyProfileApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CompanyProfileApiService> _logger;

    public CompanyProfileApiService(
        HttpClient httpClient,
        ILogger<CompanyProfileApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<CompanyProfileApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/company/profile?actorUserId={actorUserId}"),
            cancellationToken);
    }

    public Task<CompanyProfileApiResult> SaveAsync(
        int actorUserId,
        CompanyProfileInput profile,
        CancellationToken cancellationToken = default)
    {
        var request = new BackendSaveCompanyProfileRequest
        {
            ActorUserId = actorUserId,
            CompanyName = profile.CompanyName,
            CompanyType = profile.CompanyType,
            ActivityScope = profile.ActivityScope,
            FoundationYear = profile.FoundationYear,
            EmployeeCount = profile.EmployeeCount,
            Website = profile.Website,
            PageLanguage = profile.PageLanguage,
            CompanyVideo = profile.CompanyVideo,
            CompanyDescription = profile.CompanyDescription,
            CompanyCulture = profile.CompanyCulture,
            WhyWorkWithUs = profile.WhyWorkWithUs,
            Benefits = profile.Benefits ?? [],
            CompanyAddress = profile.CompanyAddress,
            CompanyCountry = profile.CompanyCountry,
            CompanyCity = profile.CompanyCity,
            LinkedInUrl = profile.LinkedInUrl,
            InstagramUrl = profile.InstagramUrl,
            FacebookUrl = profile.FacebookUrl,
            YoutubeUrl = profile.YoutubeUrl,
            TelegramUrl = profile.TelegramUrl,
            TiktokUrl = profile.TiktokUrl
        };

        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Put,
                "api/company/profile")
            {
                Content = JsonContent.Create(request)
            },
            cancellationToken);
    }

    private async Task<CompanyProfileApiResult> SendAsync(
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
                CompanyProfileApiResponse? apiResponse = null;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        apiResponse = JsonSerializer.Deserialize<
                            CompanyProfileApiResponse>(body, JsonOptions);
                    }
                    catch (JsonException exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Company profile API cavabı JSON kimi oxunmadı.");
                    }
                }

                if (apiResponse is not null)
                    return CompanyProfileApiResult.From(apiResponse);

                return CompanyProfileApiResult.Fail(
                    response.IsSuccessStatusCode
                        ? "Company profile cavabı oxunmadı."
                        : $"Company profile sorğusu tamamlanmadı. HTTP {(int)response.StatusCode}.");
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyProfileApiResult.Fail(
                "Company profile sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Company profile BackendApp ilə sinxronlaşdırılmadı.");

            return CompanyProfileApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }
}
