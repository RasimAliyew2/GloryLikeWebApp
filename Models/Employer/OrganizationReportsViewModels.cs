namespace GloryLikeWebApp.Models.Employer;

public abstract class EmployerReportsShellViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public string Initials => CompanyTeamPageViewModel.InitialsFrom(
        string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName,
        "EM");
}

public sealed class OrganizationReportsPageViewModel
    : EmployerReportsShellViewModel
{
    public List<OrganizationReportDefinitionViewModel> Reports { get; set; } = [];
}

public sealed class VacancyCreationReportPageViewModel
    : EmployerReportsShellViewModel
{
    public const string EmployeeEmailField = "employee-email";
    public const string EmployeeRoleField = "employee-role";
    public const string VacancyCountField = "vacancy-count";
    public const string EmployeeDatesField = "employee-dates";
    public const string VacanciesField = "vacancies";
    public const string VacancyDateField = "vacancy-date";
    public const string VacancyStatusField = "vacancy-status";

    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public bool WasExecuted { get; set; }
    public string ReportTitle { get; set; } =
        "Vacancies created by employees";
    public DateTime? GeneratedAtUtc { get; set; }
    public int TotalVacancyCount { get; set; }
    public HashSet<string> SelectedFields { get; set; } =
        DefaultFields();
    public List<VacancyCreatorReportRowViewModel> Employees { get; set; } = [];

    public bool Shows(string field) => SelectedFields.Contains(field);

    public static HashSet<string> DefaultFields() =>
    [
        EmployeeEmailField,
        EmployeeRoleField,
        VacancyCountField,
        EmployeeDatesField,
        VacanciesField,
        VacancyDateField,
        VacancyStatusField
    ];
}

public sealed class VacancyCreationReportQuery
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool Execute { get; set; }
    public List<string> Fields { get; set; } = [];
}

public sealed class ReportEmployeeProfilePageViewModel
    : EmployerReportsShellViewModel
{
    public ReportEmployeeProfileApiResponse? Employee { get; set; }
}

public sealed class OrganizationReportCatalogApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public List<OrganizationReportDefinitionViewModel> Reports { get; set; } = [];
}

public sealed class OrganizationReportDefinitionViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class VacancyCreationReportApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int TotalVacancyCount { get; set; }
    public List<VacancyCreatorReportRowViewModel> Employees { get; set; } = [];
}

public sealed class VacancyCreatorReportRowViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public int VacancyCount { get; set; }
    public List<DateTime> VacancyCreationDatesUtc { get; set; } = [];
    public List<VacancyCreationReportItemViewModel> Vacancies { get; set; } = [];
}

public sealed class VacancyCreationReportItemViewModel
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ReportEmployeeProfileApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string About { get; set; } = string.Empty;
    public string ProfileImageDataUrl { get; set; } = string.Empty;
    public int CreatedVacancyCount { get; set; }

    public string Initials => EmployerCandidatePageViewModel.InitialsFor(
        DisplayName,
        "TM");
}
