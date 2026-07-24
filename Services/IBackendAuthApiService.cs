using GloryLikeWebApp.Models.Auth;

namespace GloryLikeWebApp.Services;

public interface IBackendAuthApiService
{
    Task<EmailRegistrationResponseDto> StartEmailRegistrationAsync(
        RegistrationViewModel model,
        CancellationToken cancellationToken = default);

    Task<EmailRegistrationResponseDto> GetEmailRegistrationStatusAsync(
        Guid verificationId,
        CancellationToken cancellationToken = default);

    Task<EmailRegistrationResponseDto> VerifyEmailRegistrationAsync(
        Guid verificationId,
        string code,
        CancellationToken cancellationToken = default);

    Task<EmailRegistrationResponseDto> ResendEmailRegistrationCodeAsync(
        Guid verificationId,
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken = default);
}
