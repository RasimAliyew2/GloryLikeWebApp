using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models;

public class AddSkillRequest
{
    [Required(ErrorMessage = "Skill seçilməlidir.")]
    public string SelectionKey { get; set; } = string.Empty;
}
