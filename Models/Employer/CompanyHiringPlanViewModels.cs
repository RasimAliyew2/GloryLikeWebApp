using System.ComponentModel.DataAnnotations;
using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Models.Employer;

public sealed class CompanyHiringPlanPageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<JobFamily> JobFamilies { get; set; } = new();
    public List<CompanyHiringPlanItem> Plans { get; set; } = new();

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName;
            var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();
            return parts.Count == 0
                ? "EM"
                : string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
        }
    }
}

public class SaveCompanyHiringPlanInput
{
    [Range(1, int.MaxValue)]
    public int JobFamilyId { get; set; }

    [Range(1, int.MaxValue)]
    public int PositionId { get; set; }

    [Range(1, int.MaxValue)]
    public int SeniorityId { get; set; }

    [Range(1, 1000)]
    public int Headcount { get; set; } = 1;

    [RegularExpression("^(Critical|High|Medium|Low)$")]
    public string Priority { get; set; } = "Medium";

    public DateTime? TargetStartDate { get; set; }

    [RegularExpression("^(Full-time|Part-time|Contract|Temporary|Internship)$")]
    public string EmploymentType { get; set; } = "Full-time";

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public sealed class CompanyHiringPlanApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public CompanyHiringPlanItem? Plan { get; set; }
    public List<CompanyHiringPlanItem> Plans { get; set; } = new();
}

public sealed class CompanyHiringPlanItem
{
    public int Id { get; set; }
    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public int SeniorityId { get; set; }
    public string SeniorityName { get; set; } = string.Empty;
    public int Headcount { get; set; }
    public string Priority { get; set; } = string.Empty;
    public DateTime? TargetStartDate { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int VacancyCount { get; set; }
    public int FinishedVacancyCount { get; set; }
    public int RemainingVacancyCount { get; set; }
    public bool CanCreateVacancy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<CompanyHiringPlanVacancyItem> Vacancies { get; set; } = new();
}

public sealed class CompanyHiringPlanVacancyItem
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

internal sealed class BackendSaveCompanyHiringPlanRequest : SaveCompanyHiringPlanInput
{
    public int ActorUserId { get; set; }
}
