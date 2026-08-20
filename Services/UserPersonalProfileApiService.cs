using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public sealed class UserPersonalProfileApiService
    : IUserPersonalProfileApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;

    public UserPersonalProfileApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserPersonalProfileApiResult> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return UserPersonalProfileApiResult.Fail("User ID düzgün deyil.");

        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/user-personal-profile/{userId}",
                cancellationToken);
            return await ReadResultAsync(response, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UserPersonalProfileApiResult.Fail("Profil sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            return UserPersonalProfileApiResult.Fail(
                "Profil backend-inə qoşulmaq olmadı: " + exception.Message);
        }
        catch (JsonException exception)
        {
            return UserPersonalProfileApiResult.Fail(
                "Profil cavabı uyğun JSON formatında deyil: " + exception.Message);
        }
    }

    public async Task<UserPersonalProfileApiResult> UpdateAsync(
        int userId,
        UserPersonalProfileInput input,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return UserPersonalProfileApiResult.Fail("User ID düzgün deyil.");

        try
        {
            using var response = await _httpClient.PutAsJsonAsync(
                $"api/user-personal-profile/{userId}",
                input,
                JsonOptions,
                cancellationToken);
            return await ReadResultAsync(response, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UserPersonalProfileApiResult.Fail("Profilin saxlanma vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            return UserPersonalProfileApiResult.Fail(
                "Profil backend-ə göndərilmədi: " + exception.Message);
        }
        catch (JsonException exception)
        {
            return UserPersonalProfileApiResult.Fail(
                "Profil cavabı uyğun JSON formatında deyil: " + exception.Message);
        }
    }

    private static async Task<UserPersonalProfileApiResult> ReadResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = string.IsNullOrWhiteSpace(body)
            ? null
            : JsonSerializer.Deserialize<UserPersonalProfileApiResponse>(
                body,
                JsonOptions);

        if (!response.IsSuccessStatusCode || data is null || !data.Success)
        {
            return UserPersonalProfileApiResult.Fail(
                data?.Message
                ?? $"Profil əməliyyatı alınmadı. HTTP {(int)response.StatusCode}.");
        }

        return UserPersonalProfileApiResult.Ok(data);
    }
}
