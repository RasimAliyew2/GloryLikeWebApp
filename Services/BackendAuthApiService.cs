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
}
