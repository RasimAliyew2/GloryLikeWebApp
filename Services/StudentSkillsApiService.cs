using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Student;

namespace GloryLikeWebApp.Services;

public sealed class StudentSkillsApiService(HttpClient client, ILogger<StudentSkillsApiService> logger)
{
    public Task<SkillAssessmentApiResult<List<UserSkillInfo>>> GetAsync(int userId, CancellationToken ct) =>
        SendAsync<List<UserSkillInfo>>(HttpMethod.Get, $"api/StudentSkills/{userId}", null, ct);

    public Task<SkillAssessmentApiResult<UserSkillInfo>> AddAsync(int userId, int skillId, CancellationToken ct) =>
        SendAsync<UserSkillInfo>(HttpMethod.Post, $"api/StudentSkills/{userId}", new { skillId }, ct);

    public Task<SkillAssessmentApiResult<bool>> RemoveAsync(int userId, int skillId, CancellationToken ct) =>
        SendAsync<bool>(HttpMethod.Delete, $"api/StudentSkills/{userId}/{skillId}", null, ct);

    public Task<SkillAssessmentApiResult<StudentSkillQuiz>> GenerateAsync(int userId, int skillId, string language, CancellationToken ct) =>
        SendAsync<StudentSkillQuiz>(HttpMethod.Post, $"api/StudentSkills/{userId}/{skillId}/quiz", new { language }, ct);

    public Task<SkillAssessmentApiResult<StudentSkillQuizResult>> SubmitAsync(int userId, CompleteSkillAssessmentRequest input, CancellationToken ct) =>
        SendAsync<StudentSkillQuizResult>(HttpMethod.Post, $"api/StudentSkills/{userId}/{input.SkillId}/quiz/submit",
            new { input.QuestionnaireId, input.Answers }, ct);

    private async Task<SkillAssessmentApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var message = "Your skills could not be updated or loaded. Please try again.";
                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Conflict)
                {
                    using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (error.RootElement.TryGetProperty("message", out var detail) && detail.ValueKind == JsonValueKind.String)
                        message = detail.GetString() ?? message;
                }
                return SkillAssessmentApiResult<T>.Fail(message);
            }
            if (response.StatusCode == HttpStatusCode.NoContent && typeof(T) == typeof(bool))
                return SkillAssessmentApiResult<T>.Ok((T)(object)true);
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return data is null ? SkillAssessmentApiResult<T>.Fail("The skill response was empty. Please try again.")
                : SkillAssessmentApiResult<T>.Ok(data);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException || ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Student skills request failed.");
            return SkillAssessmentApiResult<T>.Fail("Skills and quizzes are temporarily unavailable. Please try again.");
        }
    }
}
