using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public sealed class UserProfileDataApiService : IUserProfileDataApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;

    public UserProfileDataApiService(HttpClient httpClient)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));
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
                return UserProfileDataApiResult.Ok(
                    new UserProfileDataResponse
                    {
                        UserId = userId,
                        Skills = new List<UserSkillInfo>(),
                        Experiences = new List<UserWorkExperienceInfo>()
                    });
            }

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

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
            {
                return UserProfileDataApiResult.Fail(
                    "Backend cavabı oxunmadı.");
            }

            data.Skills ??= new List<UserSkillInfo>();
            data.Experiences ??= new List<UserWorkExperienceInfo>();

            return UserProfileDataApiResult.Ok(data);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
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

    public async Task<UserProfileDataApiResult> SaveAsync(
        int userId,
        IReadOnlyCollection<UserSkillInfo> skills,
        IReadOnlyCollection<UserWorkExperienceInfo> experiences,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return UserProfileDataApiResult.Fail("User ID düzgün deyil.");

        var request = new SaveUserProfileDataRequest
        {
            UserId = userId,
            Skills = skills
                .Where(x => !string.IsNullOrWhiteSpace(x.SkillName))
                .GroupBy(
                    x => x.SkillName.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(x => ToDto(x.First()))
                .ToList(),
            Experiences = experiences
                .Where(x => !string.IsNullOrWhiteSpace(x.CompanyName))
                .Select(ToDto)
                .ToList()
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/UserProfileData/save",
                request,
                JsonOptions,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return UserProfileDataApiResult.Fail(
                    ExtractMessage(
                        body,
                        response.ReasonPhrase
                        ?? "Profile məlumatı SQL-də saxlanmadı."));
            }

            var data = JsonSerializer.Deserialize<UserProfileDataResponse>(
                body,
                JsonOptions)
                ?? new UserProfileDataResponse
                {
                    Success = true,
                    UserId = userId,
                    Message = "Profile məlumatı SQL-də saxlandı.",
                    Skills = request.Skills.Select(ToModel).ToList(),
                    Experiences = request.Experiences.Select(ToModel).ToList()
                };

            data.Skills ??= request.Skills.Select(ToModel).ToList();
            data.Experiences ??= request.Experiences.Select(ToModel).ToList();

            return UserProfileDataApiResult.Ok(data);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return UserProfileDataApiResult.Fail(
                "Save sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException ex)
        {
            return UserProfileDataApiResult.Fail(
                "Profile məlumatı backend-ə göndərilmədi: " + ex.Message);
        }
        catch (JsonException ex)
        {
            return UserProfileDataApiResult.Fail(
                "Save cavabı uyğun JSON formatında deyil: " + ex.Message);
        }
    }

    private static UserSkillProfileDto ToDto(UserSkillInfo skill)
    {
        return new UserSkillProfileDto
        {
            SkillId = skill.SkillId,
            SkillName = skill.SkillName,
            PositionId = skill.PositionId,
            PositionName = skill.PositionName,
            SeniorityId = skill.SeniorityId,
            SeniorityName = skill.SeniorityName,
            JobFamilyId = skill.JobFamilyId,
            JobFamilyName = skill.JobFamilyName,
            SkillComplexity = string.IsNullOrWhiteSpace(
                skill.SkillComplexity)
                ? "medium"
                : skill.SkillComplexity,
            Status = string.IsNullOrWhiteSpace(skill.Status)
                ? "self_declared"
                : skill.Status,
            IsVerified = skill.IsVerified,
            KnowledgeScore = skill.KnowledgeScore,
            ExperienceScore = skill.ExperienceScore,
            DepthScore = skill.DepthScore,
            CredibilityScore = skill.CredibilityScore,
            TaskComplexity = skill.TaskComplexity,
            OwnershipLevel = skill.OwnershipLevel,
            DepthTier = skill.DepthTier,
            ContextScore = skill.ContextScore,
            ComplexityScore = skill.ComplexityScore,
            OwnershipScore = skill.OwnershipScore,
            ResultScore = skill.ResultScore
        };
    }

    private static UserWorkExperienceProfileDto ToDto(
        UserWorkExperienceInfo experience)
    {
        return new UserWorkExperienceProfileDto
        {
            CompanyName = experience.CompanyName,
            PositionName = experience.PositionName,
            StartYear = experience.StartYear,
            EndYear = experience.EndYear,
            FileName = experience.FileName
        };
    }

    private static UserSkillInfo ToModel(UserSkillProfileDto skill)
    {
        return new UserSkillInfo
        {
            SkillId = skill.SkillId,
            SkillName = skill.SkillName,
            PositionId = skill.PositionId,
            PositionName = skill.PositionName,
            SeniorityId = skill.SeniorityId,
            SeniorityName = skill.SeniorityName,
            JobFamilyId = skill.JobFamilyId,
            JobFamilyName = skill.JobFamilyName,
            SkillComplexity = skill.SkillComplexity,
            Status = skill.Status,
            IsVerified = skill.IsVerified,
            KnowledgeScore = skill.KnowledgeScore,
            ExperienceScore = skill.ExperienceScore,
            DepthScore = skill.DepthScore,
            CredibilityScore = skill.CredibilityScore,
            TaskComplexity = skill.TaskComplexity,
            OwnershipLevel = skill.OwnershipLevel,
            DepthTier = skill.DepthTier,
            ContextScore = skill.ContextScore,
            ComplexityScore = skill.ComplexityScore,
            OwnershipScore = skill.OwnershipScore,
            ResultScore = skill.ResultScore
        };
    }

    private static UserWorkExperienceInfo ToModel(
        UserWorkExperienceProfileDto experience)
    {
        return new UserWorkExperienceInfo
        {
            CompanyName = experience.CompanyName,
            PositionName = experience.PositionName,
            StartYear = experience.StartYear,
            EndYear = experience.EndYear,
            FileName = experience.FileName
        };
    }

    private static string ExtractMessage(
        string? body,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty(
                    "message",
                    out var message))
            {
                return message.GetString() ?? fallback;
            }

            if (document.RootElement.TryGetProperty(
                    "title",
                    out var title))
            {
                return title.GetString() ?? fallback;
            }
        }
        catch
        {
            return body.Trim();
        }

        return fallback;
    }
}

public sealed class SaveUserProfileDataRequest
{
    public int UserId { get; set; }
    public List<UserSkillProfileDto> Skills { get; set; } = new();
    public List<UserWorkExperienceProfileDto> Experiences { get; set; } = new();
}

public sealed class UserProfileDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }

    public List<UserSkillInfo>? Skills { get; set; } = new();
    public List<UserWorkExperienceInfo>? Experiences { get; set; } = new();
}

public sealed class UserSkillProfileDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;

    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;

    public int SeniorityId { get; set; }
    public string SeniorityName { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;

    public string SkillComplexity { get; set; } = "medium";
    public string Status { get; set; } = "self_declared";
    public bool IsVerified { get; set; }

    public double KnowledgeScore { get; set; }
    public double ExperienceScore { get; set; }
    public double DepthScore { get; set; }
    public double CredibilityScore { get; set; }

    public string TaskComplexity { get; set; } = string.Empty;
    public string OwnershipLevel { get; set; } = string.Empty;
    public string DepthTier { get; set; } = string.Empty;

    public double ContextScore { get; set; }
    public double ComplexityScore { get; set; }
    public double OwnershipScore { get; set; }
    public double ResultScore { get; set; }
}

public sealed class UserWorkExperienceProfileDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string StartYear { get; set; } = string.Empty;
    public string EndYear { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public sealed class UserProfileDataApiResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public UserProfileDataResponse? Data { get; private set; }

    public static UserProfileDataApiResult Ok(
        UserProfileDataResponse data)
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
