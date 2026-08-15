using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models;

public sealed class AddJobRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Job seçilməlidir.")]
    public int JobFamilyId { get; set; }
}
