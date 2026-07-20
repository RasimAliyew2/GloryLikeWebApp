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
