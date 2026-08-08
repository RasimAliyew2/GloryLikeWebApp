using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Employer;

public sealed class CompanyTeamPageViewModel
{
    public int UserId { get; set; }

    public string DisplayName { get; set; } = "Employer";

    public string Email { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public bool CanManageTeam { get; set; }

    public List<CompanyTeamMemberViewModel> Members { get; set; } = [];

    public int HrAdminCount =>
        Members.Count(item => item.Role == "HR Admin");

    public int AdminCount =>
        Members.Count(item => item.Role == "Admin");

    public int HiringManagerCount =>
        Members.Count(item => item.Role == "Hiring Manager");

    public int RecruiterCount =>
        Members.Count(item => item.Role == "Recruiter");

    public string Initials =>
        InitialsFrom(
            string.IsNullOrWhiteSpace(DisplayName)
                ? Email
                : DisplayName,
            "EM");

    internal static string InitialsFrom(
        string? value,
        string fallback = "TM")
    {
        var parts = (value ?? string.Empty)
            .Split(
                new[] { ' ', '.', '_', '-', '@' },
                StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .ToList();

        return parts.Count == 0
            ? fallback
            : string.Concat(
                parts.Select(
                    part =>
                        char.ToUpperInvariant(part[0])));
    }
}

public sealed class CompanyTeamMemberViewModel
{
    public Guid InvitationId { get; set; }

    public int? UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime InvitedAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }

    public bool IsFounder { get; set; }

    public bool IsInvited =>
        string.Equals(
            Status,
            "Invited",
            StringComparison.OrdinalIgnoreCase);

    public string Initials =>
        CompanyTeamPageViewModel.InitialsFrom(
            IsInvited
                ? Email
                : DisplayName);
}

public sealed class InviteCompanyTeamMemberViewModel
{
    [Required(ErrorMessage = "Email daxil edin.")]
    [EmailAddress(ErrorMessage = "Email formatı düzgün deyil.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role seçin.")]
    [RegularExpression(
        "^(HR Admin|Hiring Manager|Recruiter)$",
        ErrorMessage = "Role düzgün deyil.")]
    public string Role { get; set; } = "Recruiter";
}

public sealed class CompanyTeamApiResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public bool CanManageTeam { get; set; }

    public CompanyTeamMemberApiItem? Member { get; set; }

    public List<CompanyTeamMemberApiItem> Members { get; set; } = [];
}

public sealed class CompanyTeamMemberApiItem
{
    public Guid InvitationId { get; set; }

    public int? UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime InvitedAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }

    public bool IsFounder { get; set; }
}

public sealed class ResolveCompanyTeamInvitationApiResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string? CompanyType { get; set; }

    public string? Industry { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}

internal sealed class BackendInviteCompanyTeamMemberRequest
{
    public int OwnerUserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
