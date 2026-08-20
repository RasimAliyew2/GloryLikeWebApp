using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public interface IUserPersonalProfileApiService
{
    Task<UserPersonalProfileApiResult> GetAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<UserPersonalProfileApiResult> UpdateAsync(
        int userId,
        UserPersonalProfileInput input,
        CancellationToken cancellationToken = default);
}
