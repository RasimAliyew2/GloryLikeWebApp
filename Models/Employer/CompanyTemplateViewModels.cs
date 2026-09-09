using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Employer;

public sealed class CompanyTemplatesPageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool CanManageTemplates { get; set; }
    public List<CompanyTemplateItem> Templates { get; set; } = [];
    public List<string> Variables { get; set; } = [];

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

public class SaveCompanyTemplateInput
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Candidate|Hiring Manager|Recruiter)$")]
    public string Audience { get; set; } = "Candidate";

    [Required]
    [StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(10000)]
    public string Body { get; set; } = string.Empty;
}

public sealed class CompanyTemplateApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public bool CanManageTemplates { get; set; }
    public CompanyTemplateItem? Template { get; set; }
    public List<CompanyTemplateItem> Templates { get; set; } = [];
    public List<string> Variables { get; set; } = [];
}

public sealed class CompanyTemplateItem
{
    public Guid Id { get; set; }
    public string DefaultKey { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}

internal sealed class BackendSaveCompanyTemplateRequest : SaveCompanyTemplateInput
{
    public int ActorUserId { get; set; }
}
