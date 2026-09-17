using System.Net.Http.Json;
using GloryLikeWebApp.Models.Student;
namespace GloryLikeWebApp.Services;

public sealed class StudentProfileApiService(HttpClient client, ILogger<StudentProfileApiService> logger)
{
    public Task<StudentProfileResponse> GetAsync(int userId, CancellationToken ct) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, $"api/students/{userId}/profile"), ct);
    public Task<StudentProfileResponse> SaveAsync(int userId, StudentEducationInput input, CancellationToken ct) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, $"api/students/{userId}/profile") { Content = JsonContent.Create(input) }, ct);

    private async Task<StudentProfileResponse> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using (request)
            using (var response = await client.SendAsync(request, ct))
            {
                if (!response.IsSuccessStatusCode) return new() { Message = "Education details could not be saved or loaded. Please try again." };
                return await response.Content.ReadFromJsonAsync<StudentProfileResponse>(cancellationToken: ct)
                    ?? new() { Message = "Education details are unavailable." };
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException || ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Student profile request failed.");
            return new() { Message = "Education details are unavailable. Please try again." };
        }
    }
}
