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
    public string? CompanyType { get; set; }

    [StringLength(120)]
    public string? ActivityScope { get; set; }

    [Range(1800, 2100, ErrorMessage = "Foundation year must be between 1800 and 2100.")]
    public int? FoundationYear { get; set; }

    [StringLength(30)]
    public string? EmployeeCount { get; set; }

    [StringLength(240)]
    public string? Website { get; set; }

    [StringLength(40)]
    public string? PageLanguage { get; set; }

    [StringLength(240)]
    public string? CompanyVideo { get; set; }

    [StringLength(2500)]
    public string? CompanyDescription { get; set; }

    [StringLength(1600)]
    public string? CompanyCulture { get; set; }

    [StringLength(1600)]
    public string? WhyWorkWithUs { get; set; }
    public List<string>? Benefits { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyCountry { get; set; }
    public string? CompanyCity { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? TelegramUrl { get; set; }
    public string? TiktokUrl { get; set; }
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
