using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Auth;

namespace GloryLikeWebApp.Services;

public class BackendAuthApiService : IBackendAuthApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<BackendAuthApiService> _logger;

    public BackendAuthApiService(
        HttpClient httpClient,
        ILogger<BackendAuthApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<EmailRegistrationResponseDto>
        StartEmailRegistrationAsync(
            RegistrationViewModel model,
            CancellationToken cancellationToken = default)
    {
        return PostEmailRegistrationAsync(
            "api/Auth/register/email/start",
            new BackendStartEmailRegistrationRequest
            {
                ProfileName = model.ProfileName.Trim(),
                Email = model.Email.Trim(),
                Password = model.Password,
                AccountType = model.AccountType,
                CompanyType = model.CompanyType,
                Industry = model.Industry,
                AcceptedTerms = model.AcceptedTerms
            },
            cancellationToken);
    }

    public Task<EmailRegistrationResponseDto>
        GetEmailRegistrationStatusAsync(
            Guid verificationId,
            CancellationToken cancellationToken = default)
    {
        return GetEmailRegistrationAsync(
            $"api/Auth/register/email/{verificationId:D}/status",
            cancellationToken);
    }

    public Task<EmailRegistrationResponseDto>
        VerifyEmailRegistrationAsync(
            Guid verificationId,
            string code,
            CancellationToken cancellationToken = default)
    {
        return PostEmailRegistrationAsync(
            "api/Auth/register/email/verify",
            new BackendVerifyEmailRegistrationRequest
            {
                VerificationId = verificationId,
                Code = code.Trim()
            },
            cancellationToken);
    }

    public Task<EmailRegistrationResponseDto>
        ResendEmailRegistrationCodeAsync(
            Guid verificationId,
            CancellationToken cancellationToken = default)
    {
        return PostEmailRegistrationAsync(
            "api/Auth/register/email/resend",
            new BackendResendEmailRegistrationRequest
            {
                VerificationId = verificationId
            },
            cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/Auth/login",
                new BackendLoginRequest
                {
                    Login = login.Trim(),
                    Password = password
                },
                cancellationToken);

            AuthResponseDto? result = null;

            try
            {
                result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(
                    JsonOptions,
                    cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Backend login response JSON formatında oxunmadı.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Password və ya login səhvdir."
                };
            }

            if (result is null || !result.Success || result.User is null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Password və ya login səhvdir."
                };
            }

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Backend cavab vermədi. Bir az sonra yenidən yoxlayın."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GloryLike Backend API-yə qoşulmaq mümkün olmadı.");

            return new AuthResponseDto
            {
                Success = false,
                Message = "Backend serverə qoşulmaq mümkün olmadı. BackendApp-in işlədiyini yoxlayın."
            };
        }
    }

    private async Task<EmailRegistrationResponseDto>
        PostEmailRegistrationAsync<TRequest>(
            string endpoint,
            TRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    endpoint,
                    request,
                    cancellationToken);

            return await ReadEmailRegistrationResponseAsync(
                response,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return BackendUnavailableEmailRegistrationResponse(
                "Backend cavab vermədi. Bir az sonra yenidən yoxlayın.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "GloryLike Backend registration API-yə qoşulmaq mümkün olmadı.");

            return BackendUnavailableEmailRegistrationResponse(
                "Backend serverə qoşulmaq mümkün olmadı. BackendApp-in işlədiyini yoxlayın.");
        }
    }

    private async Task<EmailRegistrationResponseDto>
        GetEmailRegistrationAsync(
            string endpoint,
            CancellationToken cancellationToken)
    {
        try
        {
            using var response =
                await _httpClient.GetAsync(
                    endpoint,
                    cancellationToken);

            return await ReadEmailRegistrationResponseAsync(
                response,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return BackendUnavailableEmailRegistrationResponse(
                "Backend cavab vermədi. Bir az sonra yenidən yoxlayın.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "GloryLike Backend registration status API-yə qoşulmaq mümkün olmadı.");

            return BackendUnavailableEmailRegistrationResponse(
                "Backend serverə qoşulmaq mümkün olmadı. BackendApp-in işlədiyini yoxlayın.");
        }
    }

    private async Task<EmailRegistrationResponseDto>
        ReadEmailRegistrationResponseAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await response.Content
                    .ReadFromJsonAsync<EmailRegistrationResponseDto>(
                        JsonOptions,
                        cancellationToken);

            if (result is not null)
                return result;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Backend registration response JSON formatında oxunmadı.");
        }

        return new EmailRegistrationResponseDto
        {
            Success = false,
            ErrorCode = "invalid_backend_response",
            Message = response.IsSuccessStatusCode
                ? "Backend-dən boş cavab gəldi."
                : "Qeydiyyat sorğusu tamamlanmadı."
        };
    }

    private static EmailRegistrationResponseDto
        BackendUnavailableEmailRegistrationResponse(
            string message)
    {
        return new EmailRegistrationResponseDto
        {
            Success = false,
            ErrorCode = "backend_unavailable",
            Message = message
        };
    }
}
