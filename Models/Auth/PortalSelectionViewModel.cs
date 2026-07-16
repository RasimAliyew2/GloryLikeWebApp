using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Auth;

public sealed class PortalSelectionViewModel
{
    [Required]
    public string PortalType { get; set; } = string.Empty;
}
