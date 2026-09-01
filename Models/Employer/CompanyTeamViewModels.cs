using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Employer;

public sealed class CompanyTeamPageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = string.Empty;
    public string ActiveTab { get; set; } = "participants";
    public bool CanManageTeam { get; set; }
    public bool CanManageRoles { get; set; }
    public bool CanInvite { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public List<CompanyTeamMemberViewModel> Members { get; set; } = [];
    public List<CompanyAccessRoleViewModel> Roles { get; set; } = [];
    public List<CompanyAccessHistoryViewModel> History { get; set; } = [];
    public List<CompanyPermissionGroupViewModel> PermissionGroups { get; set; } = [];

    public string Initials => InitialsFrom(
        string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName,
        "EM");

    internal static string InitialsFrom(string? value, string fallback = "TM")
    {
        var parts = (value ?? string.Empty)
            .Split(new[] { ' ', '.', '_', '-', '@' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .ToList();
        return parts.Count == 0
            ? fallback
            : string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
    }
}

public sealed class CompanyTeamMemberViewModel
{
    public Guid InvitationId { get; set; }
    public int? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime InvitedAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public bool IsFounder { get; set; }
    public bool CanChangeRole { get; set; }
    public bool CanRemove { get; set; }
    public List<string> AllowedRoles { get; set; } = [];
    public bool IsInvited => string.Equals(Status, "Invited", StringComparison.OrdinalIgnoreCase);
    public string Initials => CompanyTeamPageViewModel.InitialsFrom(
        IsInvited ? Email : DisplayName);
    public string ScopeLabel => Scope switch
    {
        "departments" => "Departments",
        "designated" => "Only designated",
        _ => "The whole company"
    };
}

public sealed class CompanyAccessRoleViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsFullAccess { get; set; }
    public int ParticipantCount { get; set; }
    public List<string> PermissionKeys { get; set; } = [];
    public int PermissionCount => PermissionKeys.Count;
    public string ScopeLabel => Scope switch
    {
        "departments" => "Departments",
        "designated" => "Only designated",
        _ => "The whole company"
    };
}

public sealed class CompanyAccessHistoryViewModel
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public int ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public int? TargetUserId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string TargetEmail { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string EventLabel => EventType switch
    {
        "permission_granted" => "Access granted",
        "permission_revoked" => "Access revoked",
        "access_granted" => "Role assigned",
        "access_changed" => "Role changed",
        "access_revoked" => "Member access revoked",
        "role_created" => "Role created",
        "role_updated" => "Role updated",
        _ => "Access event"
    };
}

public sealed class CompanyPermissionGroupViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<CompanyPermissionViewModel> Permissions { get; set; } = [];
}

public sealed class CompanyPermissionViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Sensitive { get; set; }
}

public sealed class InviteCompanyTeamMemberViewModel
{
    [Required(ErrorMessage = "Email daxil edin.")]
    [EmailAddress(ErrorMessage = "Email formatı düzgün deyil.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role seçin.")]
    public Guid? RoleId { get; set; }
}

public sealed class UpdateCompanyTeamMemberRoleViewModel
{
    [Required]
    public Guid? RoleId { get; set; }
}

public sealed class SaveCompanyAccessRoleViewModel
{
    public Guid? RoleId { get; set; }

    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(company|departments|designated)$")]
    public string Scope { get; set; } = "company";

    public List<string> PermissionKeys { get; set; } = [];
}

public sealed class CompanyRoleEditorPageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public SaveCompanyAccessRoleViewModel Role { get; set; } = new();
    public List<CompanyPermissionGroupViewModel> PermissionGroups { get; set; } = [];
    public bool IsEdit => Role.RoleId.HasValue;
    public int SelectedPermissionCount => Role.PermissionKeys.Count;
    public string Initials => CompanyTeamPageViewModel.InitialsFrom(
        string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName,
        "EM");
}

public sealed class CompanyTeamApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool CanManageTeam { get; set; }
    public bool CanManageRoles { get; set; }
    public bool CanInvite { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public CompanyTeamMemberApiItem? Member { get; set; }
    public List<CompanyTeamMemberApiItem> Members { get; set; } = [];
    public List<CompanyAccessRoleApiItem> Roles { get; set; } = [];
    public List<CompanyAccessHistoryApiItem> History { get; set; } = [];
    public List<CompanyPermissionGroupApiItem> PermissionGroups { get; set; } = [];
}

public sealed class CompanyTeamMemberApiItem
{
    public Guid InvitationId { get; set; }
    public int? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime InvitedAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public bool IsFounder { get; set; }
    public bool CanChangeRole { get; set; }
    public bool CanRemove { get; set; }
    public List<string> AllowedRoles { get; set; } = [];
}

public sealed class CompanyAccessRoleApiItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsFullAccess { get; set; }
    public int ParticipantCount { get; set; }
    public List<string> PermissionKeys { get; set; } = [];
}

public sealed class CompanyAccessHistoryApiItem
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public int ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public int? TargetUserId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string TargetEmail { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CompanyPermissionGroupApiItem
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<CompanyPermissionApiItem> Permissions { get; set; } = [];
}

public sealed class CompanyPermissionApiItem
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Sensitive { get; set; }
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
    public Guid? RoleId { get; set; }
}

internal sealed class BackendUpdateCompanyTeamMemberRoleRequest
{
    public int ActorUserId { get; set; }
    public Guid? RoleId { get; set; }
}

internal sealed class BackendSaveCompanyAccessRoleRequest
{
    public int ActorUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public List<string> PermissionKeys { get; set; } = [];
}
