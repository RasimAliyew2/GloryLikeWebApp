using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public interface IUserProfileDataApiService
{
    Task<UserProfileDataApiResult> GetAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<UserProfileDataApiResult> SaveAsync(
        int userId,
        IReadOnlyCollection<UserSkillInfo> skills,
        IReadOnlyCollection<UserWorkExperienceInfo> experiences,
        CancellationToken cancellationToken = default);
}
