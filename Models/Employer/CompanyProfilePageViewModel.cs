using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Employer;

public sealed class CompanyProfilePageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public CompanyProfileInput Profile { get; set; } = new();

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName)
                ? Email
                : DisplayName;

            var parts = source
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();

            if (parts.Count == 0)
                return "EM";

            return string.Concat(
                parts.Select(part => char.ToUpperInvariant(part[0])));
        }
    }
}

public class CompanyProfileInput
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(150, ErrorMessage = "Company name cannot exceed 150 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(30)]
    public string CompanyType { get; set; } = string.Empty;

    [StringLength(120)]
    public string ActivityScope { get; set; } = string.Empty;

    [Range(1800, 2100, ErrorMessage = "Foundation year must be between 1800 and 2100.")]
    public int? FoundationYear { get; set; }

    [StringLength(30)]
    public string EmployeeCount { get; set; } = string.Empty;

    [StringLength(240)]
    public string Website { get; set; } = string.Empty;

    [StringLength(40)]
    public string PageLanguage { get; set; } = string.Empty;

    [StringLength(240)]
    public string CompanyVideo { get; set; } = string.Empty;

    [StringLength(2500)]
    public string CompanyDescription { get; set; } = string.Empty;

    [StringLength(1600)]
    public string CompanyCulture { get; set; } = string.Empty;

    [StringLength(1600)]
    public string WhyWorkWithUs { get; set; } = string.Empty;
    public List<string> Benefits { get; set; } = [];
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyCountry { get; set; } = string.Empty;
    public string CompanyCity { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string InstagramUrl { get; set; } = string.Empty;
    public string FacebookUrl { get; set; } = string.Empty;
    public string YoutubeUrl { get; set; } = string.Empty;
    public string TelegramUrl { get; set; } = string.Empty;
    public string TiktokUrl { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class CompanyProfileApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public CompanyProfileInput? Profile { get; set; }
}

internal sealed class BackendSaveCompanyProfileRequest : CompanyProfileInput
{
    public int ActorUserId { get; set; }
}
