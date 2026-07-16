using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models;

public class AddExperienceRequest
{
    [Required(ErrorMessage = "Şirkət adı boş ola bilməz.")]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position boş ola bilməz.")]
    [StringLength(200)]
    public string PositionName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Başlama ili boş ola bilməz.")]
    [StringLength(20)]
    public string StartYear { get; set; } = string.Empty;

    [StringLength(20)]
    public string EndYear { get; set; } = "Present";

    [StringLength(300)]
    public string FileName { get; set; } = string.Empty;
}
