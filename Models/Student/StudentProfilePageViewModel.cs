using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Student;

public sealed class StudentProfilePageViewModel
{
    public StudentProfileInput Input { get; set; } = new();
    public bool IsAvailable { get; set; }
    public bool Saved { get; set; }

    public int CompletionPercent
    {
        get
        {
            var completed = new[]
            {
                !string.IsNullOrWhiteSpace(Input.FirstName),
                !string.IsNullOrWhiteSpace(Input.LastName),
                !string.IsNullOrWhiteSpace(Input.University),
                !string.IsNullOrWhiteSpace(Input.Specialty),
                Input.StudyYear is >= 1 and <= 5,
                Input.GraduationYear is >= 1900 and <= 2200,
                !string.IsNullOrWhiteSpace(Input.About),
                !string.IsNullOrWhiteSpace(Input.Goal)
            }.Count(value => value);
            return (int)Math.Round(completed * 100d / 8, MidpointRounding.AwayFromZero);
        }
    }
}

public sealed class StudentProfileInput
{
    [Required, StringLength(80), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string University { get; set; } = string.Empty;

    [Required, StringLength(200), Display(Name = "Specialization")]
    public string Specialty { get; set; } = string.Empty;

    [Range(1, 5), Display(Name = "Year of study")]
    public int StudyYear { get; set; } = 1;

    [Range(1900, 2200), Display(Name = "Expected graduation year")]
    public int? GraduationYear { get; set; }

    [StringLength(2000), Display(Name = "Bio")]
    public string? About { get; set; }

    [StringLength(500), Display(Name = "Internship goal")]
    public string? Goal { get; set; }

    public bool OpenToInternships { get; set; } = true;
    [StringLength(2_796_230, ErrorMessage = "Your photo must be 2 MB or smaller.")]
    public string? ProfileImageDataUrl { get; set; }
}
