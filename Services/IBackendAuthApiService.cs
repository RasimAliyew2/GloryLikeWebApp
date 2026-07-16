using GloryLikeWebApp.Models.Auth;

namespace GloryLikeWebApp.Services;

public interface IBackendAuthApiService
{
    Task<AuthResponseDto> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken = default);
}
