using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class CompanyTeamApiService : ICompanyTeamApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CompanyTeamApiService> _logger;

    public CompanyTeamApiService(
        HttpClient httpClient,
        ILogger<CompanyTeamApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CompanyTeamApiResult> GetTeamAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId <= 0)
        {
            return CompanyTeamApiResult.Fail(
                "Employer user ID düzgün deyil.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/company/team?ownerUserId={ownerUserId}",
                cancellationToken);

            var result =
                await ReadCompanyTeamResponseAsync(
                    response,
                    cancellationToken);

            return result is null
                ? CompanyTeamApiResult.Fail(
                    "Backend team cavabı oxunmadı.")
                : CompanyTeamApiResult.From(result);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyTeamApiResult.Fail(
                "Team sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Company Team BackendApp-dən yüklənmədi.");

            return CompanyTeamApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public async Task<CompanyTeamApiResult> InviteAsync(
        int ownerUserId,
        InviteCompanyTeamMemberViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId <= 0)
        {
            return CompanyTeamApiResult.Fail(
                "Employer user ID düzgün deyil.");
        }

        try
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/company/team/invitations",
                    new BackendInviteCompanyTeamMemberRequest
                    {
                        OwnerUserId = ownerUserId,
                        Email = model.Email.Trim(),
                        Role = model.Role.Trim()
                    },
                    cancellationToken);

            var result =
                await ReadCompanyTeamResponseAsync(
                    response,
                    cancellationToken);

            return result is null
                ? CompanyTeamApiResult.Fail(
                    "Backend invitation cavabı oxunmadı.")
                : CompanyTeamApiResult.From(result);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyTeamApiResult.Fail(
                "Invitation sorğusunun vaxtı bitdi.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Team invitation BackendApp-ə göndərilmədi.");

            return CompanyTeamApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public async Task<CompanyTeamApiResult> RemoveMemberAsync(
        int actorUserId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0 || invitationId == Guid.Empty)
        {
            return CompanyTeamApiResult.Fail(
                "Team üzvü məlumatı düzgün deyil.");
        }

        try
        {
            var endpoint =
                $"api/company/team/invitations/{invitationId}"
                + $"?actorUserId={actorUserId}";

            using var response =
                await _httpClient.DeleteAsync(
                    endpoint,
                    cancellationToken);

            var result =
                await ReadCompanyTeamResponseAsync(
                    response,
                    cancellationToken);

            return result is null
                ? CompanyTeamApiResult.Fail(
                    "Backend silmə cavabı oxunmadı.")
                : CompanyTeamApiResult.From(result);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyTeamApiResult.Fail(
                "Team üzvünün silinməsi vaxtında tamamlanmadı.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Team üzvü BackendApp-dən silinmədi.");

            return CompanyTeamApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public async Task<CompanyTeamApiResult> UpdateMemberRoleAsync(
        int actorUserId,
        Guid invitationId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0
            || invitationId == Guid.Empty
            || string.IsNullOrWhiteSpace(role))
        {
            return CompanyTeamApiResult.Fail(
                "Access level məlumatı düzgün deyil.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/company/team/invitations/{invitationId}/role")
            {
                Content = JsonContent.Create(
                    new BackendUpdateCompanyTeamMemberRoleRequest
                    {
                        ActorUserId = actorUserId,
                        Role = role.Trim()
                    },
                    options: JsonOptions)
            };

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);
            var result = await ReadCompanyTeamResponseAsync(
                response,
                cancellationToken);

            return result is null
                ? CompanyTeamApiResult.Fail(
                    "Backend access level cavabı oxunmadı.")
                : CompanyTeamApiResult.From(result);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyTeamApiResult.Fail(
                "Access level yenilənməsi vaxtında tamamlanmadı.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Team access level BackendApp-də yenilənmədi.");

            return CompanyTeamApiResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    public async Task<CompanyTeamInvitationResolveResult>
        ResolveInvitationAsync(
            string token,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return CompanyTeamInvitationResolveResult.Fail(
                "Invitation link düzgün deyil.");
        }

        try
        {
            var endpoint =
                "api/company/team/invitations/resolve?token="
                + Uri.EscapeDataString(token.Trim());

            using var response =
                await _httpClient.GetAsync(
                    endpoint,
                    cancellationToken);
            var content =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);
            var result =
                Deserialize<
                    ResolveCompanyTeamInvitationApiResponse>(
                        content);

            if (result is not null)
            {
                return CompanyTeamInvitationResolveResult.From(
                    result);
            }

            return CompanyTeamInvitationResolveResult.Fail(
                response.IsSuccessStatusCode
                    ? "Backend invitation cavabı oxunmadı."
                    : "Invitation link düzgün deyil və ya vaxtı bitib.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyTeamInvitationResolveResult.Fail(
                "Invitation yoxlanışı vaxtında tamamlanmadı.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Team invitation BackendApp-də yoxlanmadı.");

            return CompanyTeamInvitationResolveResult.Fail(
                "BackendApp-ə qoşulmaq mümkün olmadı.");
        }
    }

    private async Task<CompanyTeamApiResponse?>
        ReadCompanyTeamResponseAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        var content =
            await response.Content.ReadAsStringAsync(
                cancellationToken);
        var result =
            Deserialize<CompanyTeamApiResponse>(
                content);

        if (result is not null)
        {
            result.Members ??= [];
            return result;
        }

        _logger.LogWarning(
            "Company Team API cavabı JSON kimi oxunmadı. HTTP {StatusCode}.",
            (int)response.StatusCode);

        return null;
    }

    private T? Deserialize<T>(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(
                content,
                JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Company Team API cavabı JSON kimi oxunmadı.");

            return default;
        }
    }
}
