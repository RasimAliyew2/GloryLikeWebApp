namespace GloryLikeWebApp.Models.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthUserDto? User { get; set; }
}

public class AuthUserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CompanyType { get; set; }
    public string? Industry { get; set; }
}

internal sealed class BackendLoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

internal sealed class BackendStartEmailRegistrationRequest
{
    public string ProfileName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string? CompanyType { get; set; }
    public string? Industry { get; set; }
    public bool AcceptedTerms { get; set; }
}

internal sealed class BackendVerifyEmailRegistrationRequest
{
    public Guid VerificationId { get; set; }
    public string Code { get; set; } = string.Empty;
}

internal sealed class BackendResendEmailRegistrationRequest
{
    public Guid VerificationId { get; set; }
}

public sealed class EmailRegistrationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public Guid? VerificationId { get; set; }
    public string MaskedEmail { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ResendAvailableAtUtc { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendInSeconds { get; set; }
    public bool Expired { get; set; }
    public bool CanResend { get; set; }
    public AuthUserDto? User { get; set; }
}
