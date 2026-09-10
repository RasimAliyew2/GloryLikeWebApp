using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Employer;

public sealed class CompanyFunnelsPageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool CanManageTemplates { get; set; }
    public List<CompanyFunnelItem> Templates { get; set; } = [];

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName;
            var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();
            return parts.Count == 0
                ? "EM"
                : string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
        }
    }
}

public class SaveCompanyFunnelInput
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string Description { get; set; } = string.Empty;
    [Required, MinLength(1), MaxLength(20)]
    public List<VacancyFunnelStageInput> Stages { get; set; } = [];
}

public sealed class CompanyFunnelApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public bool CanManageTemplates { get; set; }
    public CompanyFunnelItem? Template { get; set; }
    public List<CompanyFunnelItem> Templates { get; set; } = [];
}

public sealed class CompanyFunnelItem
{
    public Guid Id { get; set; }
    public string DefaultKey { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<VacancyFunnelStageInput> Stages { get; set; } = [];
    public DateTime UpdatedAtUtc { get; set; }
}

internal sealed class BackendSaveCompanyFunnelRequest : SaveCompanyFunnelInput
{
    public int ActorUserId { get; set; }
}
