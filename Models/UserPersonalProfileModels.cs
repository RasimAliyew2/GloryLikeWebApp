using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models;

public sealed class UserPersonalProfileInput
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [StringLength(1000, ErrorMessage = "About cannot exceed 1000 characters.")]
    public string? About { get; set; }

    public string? ProfileImageDataUrl { get; set; }
}

public sealed class UserPersonalProfileApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string About { get; set; } = string.Empty;
    public string ProfileImageDataUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
}

public sealed class UserPersonalProfileApiResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public UserPersonalProfileApiResponse? Data { get; init; }

    public static UserPersonalProfileApiResult Ok(
        UserPersonalProfileApiResponse data)
    {
        return new UserPersonalProfileApiResult
        {
            Success = true,
            Message = data.Message,
            Data = data
        };
    }

    public static UserPersonalProfileApiResult Fail(string message)
    {
        return new UserPersonalProfileApiResult
        {
            Success = false,
            Message = message
        };
    }
}
