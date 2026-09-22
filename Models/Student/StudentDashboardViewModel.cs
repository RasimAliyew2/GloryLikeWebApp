using System.ComponentModel.DataAnnotations;
namespace GloryLikeWebApp.Models.Student;

public sealed class StudentEducationInput
{
    [Required, StringLength(200)] public string University { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Specialty { get; set; } = string.Empty;
    [Range(1, 5)] public int StudyYear { get; set; } = 1;
}
public sealed class StudentProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public int? StudyYear { get; set; }
    public bool HasEducation => !string.IsNullOrWhiteSpace(University) && !string.IsNullOrWhiteSpace(Specialty) && StudyYear is >= 1 and <= 5;
}
public sealed record StudentAchievement(string Name, string Description, string Icon, int Progress, bool IsAvailable = true)
{
    public bool Earned => IsAvailable && Progress >= 100;
}
public sealed class StudentDashboardViewModel
{
    public string DisplayName { get; set; } = "Student";
    public string Page { get; set; } = "Overview";
    public string EventsTab { get; set; } = "upcoming";
    public StudentProfileResponse Education { get; set; } = new();
    public bool SkillsAvailable { get; set; }
    public bool ApplicationsAvailable { get; set; }
    public bool OpportunitiesAvailable { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<UserSkillInfo> Skills { get; set; } = [];
    public List<CandidateVacancyApiItem> Opportunities { get; set; } = [];
    public List<CandidateApplicationApiItem> InternshipApplications { get; set; } = [];
    public List<StudentAchievement> Achievements { get; set; } = [];
    public int Ssi { get; set; }
    public int VerifiedSkills => Skills.Count(s => s.IsVerified || string.Equals(s.Status, "verified", StringComparison.OrdinalIgnoreCase));
    public string Level => !SkillsAvailable ? "Unavailable" : Ssi >= 81 ? "Standout" : Ssi >= 61 ? "Job-Ready" : Ssi >= 41 ? "Building momentum" : "Getting started";
    public int NextTarget => Ssi >= 81 ? 100 : Ssi >= 61 ? 81 : Ssi >= 41 ? 61 : 41;
    public string NextLevel => Ssi >= 81 ? "100 SSI" : Ssi >= 61 ? "Standout" : Ssi >= 41 ? "Job-Ready" : "Building momentum";
    public string SsiText => SkillsAvailable ? Ssi.ToString() : "—";
    public string SkillCountText => SkillsAvailable ? Skills.Count.ToString() : "—";
    public string ApplicationCountText => ApplicationsAvailable ? InternshipApplications.Count.ToString() : "—";
}
