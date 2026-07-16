using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public interface IUserProfileDataApiService
{
    Task<UserProfileDataApiResult> GetAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
