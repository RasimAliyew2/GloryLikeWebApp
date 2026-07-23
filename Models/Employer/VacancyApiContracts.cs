namespace GloryLikeWebApp.Models.Employer;

public sealed class CreateVacancyApiRequest
{
    public int EmployerUserId { get; set; }
    public CreateVacancyInput Vacancy { get; set; } = new();
}

public sealed class CreateVacancyApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public DateTime? CreatedAtUtc { get; set; }
}

public sealed class EmployerVacancyListApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EmployerUserId { get; set; }
    public List<EmployerVacancyListApiItem> Vacancies { get; set; } = new();
}

public sealed class EmployerVacancyListApiItem
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class EmployerVacancyDetailApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EmployerUserId { get; set; }
    public EmployerVacancyDetailApiItem? Vacancy { get; set; }
}

public sealed class EmployerVacancyDetailApiItem
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public int EmployerUserId { get; set; }
    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;
    public string SeniorityName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PublishDate { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int ApplicantCount { get; set; }
    public int AverageMatchScore { get; set; }
    public int HighConfidenceCount { get; set; }
    public EmployerVacancyApplicantApiItem? BestMatch { get; set; }
    public List<EmployerVacancyApplicantApiItem> Applicants { get; set; } = new();
    public List<EmployerVacancySkillApiItem> Skills { get; set; } = new();
    public List<EmployerVacancyFunnelStageApiItem> FunnelStages { get; set; } = new();
}

public sealed class EmployerVacancyApplicantApiItem
{
    public int ApplicationId { get; set; }
    public int CandidateUserId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string ApplicationStatus { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
}

public sealed class EmployerVacancySkillApiItem
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Weight { get; set; }
    public string RequirementType { get; set; } = string.Empty;
}

public sealed class EmployerVacancyFunnelStageApiItem
{
    public string StageName { get; set; } = string.Empty;
    public int Hours { get; set; }
    public bool IsStandard { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ToggleEmployerVacancyStatusApiRequest
{
    public int EmployerUserId { get; set; }
}

public sealed class ToggleEmployerVacancyStatusApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int VacancyId { get; set; }
    public int EmployerUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
