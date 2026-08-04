using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public interface ICompanyTeamApiService
{
    Task<CompanyTeamApiResult> GetTeamAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyTeamApiResult> InviteAsync(
        int ownerUserId,
        InviteCompanyTeamMemberViewModel model,
        CancellationToken cancellationToken = default);

    Task<CompanyTeamInvitationResolveResult>
        ResolveInvitationAsync(
            string token,
            CancellationToken cancellationToken = default);
}

public sealed class CompanyTeamApiResult
{
    public bool Success { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public CompanyTeamApiResponse? Data { get; private set; }

    public static CompanyTeamApiResult From(
        CompanyTeamApiResponse response)
    {
        return new CompanyTeamApiResult
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };
    }

    public static CompanyTeamApiResult Fail(string message)
    {
        return new CompanyTeamApiResult
        {
            Success = false,
            Message = message
        };
    }
}

public sealed class CompanyTeamInvitationResolveResult
{
    public bool Success { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public ResolveCompanyTeamInvitationApiResponse? Data
    {
        get;
        private set;
    }

    public static CompanyTeamInvitationResolveResult From(
        ResolveCompanyTeamInvitationApiResponse response)
    {
        return new CompanyTeamInvitationResolveResult
        {
            Success = response.Success,
            Message = response.Message,
            Data = response
        };
    }

    public static CompanyTeamInvitationResolveResult Fail(
        string message)
    {
        return new CompanyTeamInvitationResolveResult
        {
            Success = false,
            Message = message
        };
    }
}
