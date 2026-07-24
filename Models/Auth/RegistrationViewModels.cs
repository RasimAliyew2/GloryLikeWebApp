using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Auth;

public sealed class RegistrationViewModel
{
    [Required(ErrorMessage = "Profil və ya şirkət adını daxil edin.")]
    [StringLength(150)]
    public string ProfileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email daxil edin.")]
    [EmailAddress(ErrorMessage = "Email formatı düzgün deyil.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password daxil edin.")]
    [MinLength(8, ErrorMessage = "Password ən azı 8 simvol olmalıdır.")]
    [MaxLength(128)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        "^(candidate|employer)$",
        ErrorMessage = "Hesab növü düzgün deyil.")]
    public string AccountType { get; set; } = "employer";

    [StringLength(30)]
    public string? CompanyType { get; set; } = "SME";

    [StringLength(120)]
    public string? Industry { get; set; }

    [Range(
        typeof(bool),
        "true",
        "true",
        ErrorMessage = "Şərtləri və məxfilik siyasətini qəbul edin.")]
    public bool AcceptedTerms { get; set; }
}

public sealed class VerifyRegistrationViewModel
{
    public Guid VerificationId { get; set; }

    [Required(ErrorMessage = "Təsdiq kodunu daxil edin.")]
    [RegularExpression(
        "^\\d{6}$",
        ErrorMessage = "Təsdiq kodu 6 rəqəmdən ibarət olmalıdır.")]
    public string Code { get; set; } = string.Empty;

    public string MaskedEmail { get; set; } = string.Empty;

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime? ResendAvailableAtUtc { get; set; }

    public int ExpiresInSeconds { get; set; }

    public int ResendInSeconds { get; set; }

    public bool Expired { get; set; }

    public bool CanResend { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public string SuccessMessage { get; set; } = string.Empty;
}
