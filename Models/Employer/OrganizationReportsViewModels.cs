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
    public const string EmployeeScope = "employee";
    public const string VacancyScope = "vacancy";
    public const string EmployeeField = "employee";
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
    public List<ReportHierarchyLevelViewModel> HierarchyLevels { get; set; } =
        DefaultHierarchy();
    public string HierarchyLayout { get; set; } = string.Empty;
    public List<VacancyCreatorReportRowViewModel> Employees { get; set; } = [];

    public bool Shows(string field) => SelectedFields.Contains(field);

    public string LabelFor(string field) => FieldDefinitionsByKey.TryGetValue(
        field,
        out var definition)
            ? definition.Label
            : field;

    public string TypeFor(string field) => FieldDefinitionsByKey.TryGetValue(
        field,
        out var definition)
            ? definition.ValueType
            : "String";

    public string EmployeeValue(
        string field,
        VacancyCreatorReportRowViewModel employee)
    {
        return field switch
        {
            EmployeeEmailField => employee.Email,
            EmployeeRoleField => employee.Role,
            VacancyCountField => employee.VacancyCount.ToString(),
            EmployeeDatesField => employee.VacancyCreationDatesUtc.Count == 0
                ? "—"
                : string.Join(
                    ", ",
                    employee.VacancyCreationDatesUtc.Select(
                        date => date.ToString("dd.MM.yyyy"))),
            VacancyDateField => employee.Vacancies.Count == 0
                ? "—"
                : string.Join(
                    ", ",
                    employee.Vacancies.Select(
                        vacancy => vacancy.CreatedAtUtc.ToString(
                            "dd.MM.yyyy HH:mm"))),
            VacancyStatusField => employee.Vacancies.Count == 0
                ? "—"
                : string.Join(
                    ", ",
                    employee.Vacancies
                        .Select(vacancy => vacancy.Status)
                        .Distinct(StringComparer.OrdinalIgnoreCase)),
            _ => string.Empty
        };
    }

    public string VacancyValue(
        string field,
        VacancyCreatorReportRowViewModel employee,
        VacancyCreationReportItemViewModel vacancy)
    {
        return field switch
        {
            EmployeeEmailField => employee.Email,
            EmployeeRoleField => employee.Role,
            VacancyCountField => employee.VacancyCount.ToString(),
            EmployeeDatesField => employee.VacancyCreationDatesUtc.Count == 0
                ? "—"
                : string.Join(
                    ", ",
                    employee.VacancyCreationDatesUtc.Select(
                        date => date.ToString("dd.MM.yyyy"))),
            VacancyDateField => vacancy.CreatedAtUtc.ToString(
                "dd.MM.yyyy HH:mm"),
            VacancyStatusField => vacancy.Status,
            _ => string.Empty
        };
    }

    public static HashSet<string> DefaultFields() =>
    [
        EmployeeField,
        EmployeeEmailField,
        EmployeeRoleField,
        VacancyCountField,
        EmployeeDatesField,
        VacanciesField,
        VacancyDateField,
        VacancyStatusField
    ];

    public static HashSet<string> AllFieldKeys() => FieldDefinitions
        .Select(definition => definition.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static List<ReportHierarchyLevelViewModel> DefaultHierarchy() =>
    [
        new ReportHierarchyLevelViewModel
        {
            Scope = EmployeeScope,
            FieldKeys =
            [
                EmployeeField,
                EmployeeEmailField,
                EmployeeRoleField,
                VacancyCountField,
                EmployeeDatesField
            ]
        },
        new ReportHierarchyLevelViewModel
        {
            Scope = VacancyScope,
            FieldKeys =
            [
                VacanciesField,
                VacancyDateField,
                VacancyStatusField
            ]
        }
    ];

    public static string DefaultScopeFor(string field) => field switch
    {
        VacanciesField or VacancyDateField or VacancyStatusField =>
            VacancyScope,
        _ => EmployeeScope
    };

    public static string SerializeHierarchy(
        IEnumerable<ReportHierarchyLevelViewModel> levels)
    {
        return string.Join(
            "|",
            levels.Select(level =>
                $"{level.Scope}:{string.Join(',', level.FieldKeys)}"));
    }

    private static readonly IReadOnlyList<ReportFieldDefinitionViewModel>
        FieldDefinitions =
        [
            new(EmployeeField, "Employee", "Custom"),
            new(EmployeeEmailField, "Email", "String"),
            new(EmployeeRoleField, "Access role", "String"),
            new(VacancyCountField, "Vacancy count", "Integer"),
            new(EmployeeDatesField, "Creation dates", "Date list"),
            new(VacanciesField, "Created vacancies", "Custom"),
            new(VacancyDateField, "Vacancy creation date", "Date"),
            new(VacancyStatusField, "Vacancy status", "String")
        ];

    private static readonly IReadOnlyDictionary<
        string,
        ReportFieldDefinitionViewModel> FieldDefinitionsByKey =
        FieldDefinitions.ToDictionary(
            definition => definition.Key,
            StringComparer.OrdinalIgnoreCase);
}

public sealed class VacancyCreationReportQuery
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool Execute { get; set; }
    public List<string> Fields { get; set; } = [];
    public string Layout { get; set; } = string.Empty;
}

public sealed class ReportHierarchyLevelViewModel
{
    public string Scope { get; set; } = string.Empty;
    public List<string> FieldKeys { get; set; } = [];
}

public sealed record ReportFieldDefinitionViewModel(
    string Key,
    string Label,
    string ValueType);

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
