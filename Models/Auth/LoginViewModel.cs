using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Login daxil edin.")]
    [Display(Name = "Email, username və ya telefon")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password daxil edin.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
