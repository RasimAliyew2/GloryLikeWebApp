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
