using System.ComponentModel.DataAnnotations;
using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Models.Employer;

public sealed class CreateVacancyPageViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;

    public List<JobFamily> JobFamilies { get; set; } = new();

    public CreateVacancyInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public bool HasTaxonomy =>
        JobFamilies.Count > 0;

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName)
                ? Email
                : DisplayName;

            var parts = source
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();

            if (parts.Count == 0)
                return "EM";

            return string.Concat(
                parts.Select(
                    part => char.ToUpperInvariant(part[0])));
        }
    }
}

public sealed class CreateVacancyInput
{
    // Step 1 — Role and Profile
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Job Family seçilməlidir.")]
    public int JobFamilyId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Seniority Level seçilməlidir.")]
    public int SeniorityId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "SQL Position seçilməlidir.")]
    public int PositionId { get; set; }

    [Required(ErrorMessage = "Role Title boş ola bilməz.")]
    [StringLength(200)]
    public string RoleTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string PlatformVacancyId { get; set; } = string.Empty;

    [StringLength(100)]
    public string ClientRequisitionCode { get; set; } = string.Empty;

    [Required]
    public string EmploymentType { get; set; } = "Full-time";

    [Required]
    public string ExperienceRequired { get; set; } = "1-3 years";

    [Required]
    public string EducationRequirement { get; set; } = "Preferred";

    [Required]
    public string EducationLevel { get; set; } = "Bachelor";

    [Range(
        0,
        1_000_000,
        ErrorMessage = "Minimum salary düzgün deyil.")]
    public decimal? MinSalary { get; set; }

    [Range(
        0,
        1_000_000,
        ErrorMessage = "Maximum salary düzgün deyil.")]
    public decimal? MaxSalary { get; set; }

    public string PaymentTerms { get; set; } = "Monthly Gross";
    public string Currency { get; set; } = "AZN";
    public bool HideSalary { get; set; }

    [StringLength(5000)]
    public string JobDescription { get; set; } = string.Empty;

    /// <summary>
    /// Yeni əsas model. Hər skill öz verification level və
    /// Required/Desirable statusunu saxlayır.
    /// </summary>
    public List<VacancySkillRequirementInput> SkillRequirements
    {
        get;
        set;
    } = new();

    /// <summary>
    /// Köhnə View və əvvəlki POST-larla compatibility üçün saxlanılıb.
    /// Yeni JavaScript bunu da hidden input kimi göndərir.
    /// </summary>
    public List<int> SelectedSkillIds { get; set; } = new();

    /// <summary>
    /// Köhnə ümumi slider ilə compatibility. Yeni sistemdə hər skill-in
    /// ayrıca MinimumVerificationLevel dəyəri var.
    /// </summary>
    [Range(1, 100)]
    public int MinimumVerificationLevel { get; set; } = 70;

    public List<string> Benefits { get; set; } = new();

    // Step 2 — Application Requirements
    public bool RequireCv { get; set; } = true;
    public bool RequireCoverLetter { get; set; }
    public bool RequirePortfolio { get; set; }
    public bool RequireLinkedIn { get; set; }
    public string NoticePeriod { get; set; } = "Any";
    public string ApplicationQuestions { get; set; } = string.Empty;

    // Step 3 — Screening
    [Range(0, 100)]
    public int MinimumMatchScore { get; set; } = 65;

    [Range(0, 100)]
    public int MinimumTrustScore { get; set; } = 60;

    public bool AutoRejectBelowScore { get; set; }
    public bool RequireVerifiedCoreSkills { get; set; } = true;
    public string ScreeningNotes { get; set; } = string.Empty;

    // Step 4 — Funnel
    public bool StageApplied { get; set; } = true;
    public bool StageScreening { get; set; } = true;
    public bool StageInterview { get; set; } = true;
    public bool StageOffer { get; set; } = true;
    public int InterviewRounds { get; set; } = 2;
    public int ScreeningSlaDays { get; set; } = 3;

    // Step 5 — Publication
    public string Visibility { get; set; } = "Public";
    public DateTime? PublishDate { get; set; }
    public DateTime? ApplicationDeadline { get; set; }

    [Required(ErrorMessage = "Contact email boş ola bilməz.")]
    [EmailAddress(ErrorMessage = "Contact email formatı düzgün deyil.")]
    public string ContactEmail { get; set; } = string.Empty;

    public bool AllowInternalCandidates { get; set; } = true;
    public bool NotifyMatchingCandidates { get; set; } = true;
}

public sealed class VacancySkillRequirementInput
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "SQL skill düzgün deyil.")]
    public int SkillId { get; set; }

    [Range(
        1,
        100,
        ErrorMessage = "Verification level 1–100 arasında olmalıdır.")]
    public int MinimumVerificationLevel { get; set; } = 70;

    [Required]
    [RegularExpression(
        "^(Required|Desirable)$",
        ErrorMessage = "Skill statusu Required və ya Desirable olmalıdır.")]
    public string RequirementType { get; set; } = "Required";
}
