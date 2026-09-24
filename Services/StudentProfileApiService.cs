using System.Net.Http.Json;
using GloryLikeWebApp.Models.Student;
namespace GloryLikeWebApp.Services;

public sealed class StudentProfileApiService(HttpClient client, ILogger<StudentProfileApiService> logger)
{
    public Task<StudentProfileResponse> GetAsync(int userId, CancellationToken ct) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, $"api/students/{userId}/profile"), ct);
    public Task<StudentProfileResponse> SaveAsync(int userId, StudentEducationInput input, CancellationToken ct) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, $"api/students/{userId}/profile") { Content = JsonContent.Create(input) }, ct);
    public Task<StudentProfileResponse> SaveProfileAsync(int userId, StudentProfileInput input, CancellationToken ct) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, $"api/students/{userId}/profile/details") { Content = JsonContent.Create(input) }, ct);

    private async Task<StudentProfileResponse> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using (request)
            using (var response = await client.SendAsync(request, ct))
            {
                if (!response.IsSuccessStatusCode)
                {
                    // Only intentional validation messages are safe to display; never expose upstream error bodies.
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var error = await response.Content.ReadFromJsonAsync<StudentProfileResponse>(cancellationToken: ct);
                        if (!string.IsNullOrWhiteSpace(error?.Message)) return new() { Message = error.Message };
                    }
                    return new() { Message = "Your profile could not be saved or loaded. Please try again." };
                }
                return await response.Content.ReadFromJsonAsync<StudentProfileResponse>(cancellationToken: ct)
                    ?? new() { Message = "Your profile is unavailable." };
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException || ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Student profile request failed.");
            return new() { Message = "Your profile is unavailable. Please try again." };
        }
    }
}
