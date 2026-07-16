using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public sealed class UserProfileDataApiService : IUserProfileDataApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public UserProfileDataApiService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<UserProfileDataApiResult> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return UserProfileDataApiResult.Fail("User ID düzgün deyil.");

        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/UserProfileData/{userId}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UserProfileDataApiResult.Fail(
                    "Backend-də UserProfileData endpoint-i tapılmadı və ya bu user üçün məlumat yoxdur.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return UserProfileDataApiResult.Fail(
                    ExtractMessage(
                        body,
                        $"Profile məlumatı yüklənmədi. HTTP {(int)response.StatusCode}."));
            }

            var data = JsonSerializer.Deserialize<UserProfileDataResponse>(
                body,
                JsonOptions);

            if (data is null)
                return UserProfileDataApiResult.Fail("Backend cavabı oxunmadı.");

            data.Skills ??= new List<UserSkillInfo>();
            data.Experiences ??= new List<UserWorkExperienceInfo>();

            return UserProfileDataApiResult.Ok(data);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UserProfileDataApiResult.Fail(
                "Backend sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException ex)
        {
            return UserProfileDataApiResult.Fail(
                "Backend-ə qoşulmaq olmadı: " + ex.Message);
        }
        catch (JsonException ex)
        {
            return UserProfileDataApiResult.Fail(
                "Backend JSON cavabı uyğun formatda deyil: " + ex.Message);
        }
    }

    private static string ExtractMessage(string? body, string fallback)
    {
        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? fallback;

            if (document.RootElement.TryGetProperty("title", out var title))
                return title.GetString() ?? fallback;
        }
        catch
        {
            return body.Trim();
        }

        return fallback;
    }
}

public sealed class UserProfileDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }

    public List<UserSkillInfo>? Skills { get; set; } = new();
    public List<UserWorkExperienceInfo>? Experiences { get; set; } = new();
}

public sealed class UserProfileDataApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public UserProfileDataResponse? Data { get; private set; }

    public static UserProfileDataApiResult Ok(UserProfileDataResponse data)
    {
        return new UserProfileDataApiResult
        {
            Success = true,
            Message = data.Message,
            Data = data
        };
    }

    public static UserProfileDataApiResult Fail(string message)
    {
        return new UserProfileDataApiResult
        {
            Success = false,
            Message = message
        };
    }
}
