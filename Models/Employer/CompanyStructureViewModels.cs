using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Employer;

public sealed class CompanyStructurePageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<CompanyStructureDepartmentItem> Departments { get; set; } = new();

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

public sealed class SaveCompanyStructureInput
{
    public List<SaveCompanyStructureDepartmentInput> Departments { get; set; } = new();
}

public sealed class SaveCompanyStructureDepartmentInput
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;
    public List<SaveCompanyStructureDivisionInput> Divisions { get; set; } = new();
}

public sealed class SaveCompanyStructureDivisionInput
{
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;
    public List<SaveCompanyStructurePositionInput> Positions { get; set; } = new();
}

public sealed class SaveCompanyStructurePositionInput
{
    [Required]
    [StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Seniority { get; set; } = "Not specified";

    [Range(1, 10000)]
    public int Headcount { get; set; } = 1;

    [StringLength(160)]
    public string ReportsTo { get; set; } = string.Empty;
}

public sealed class CompanyStructureApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public List<CompanyStructureDepartmentItem> Departments { get; set; } = new();
}

public sealed class CompanyStructureDepartmentItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CompanyStructureDivisionItem> Divisions { get; set; } = new();
}

public sealed class CompanyStructureDivisionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CompanyStructurePositionItem> Positions { get; set; } = new();
}

public sealed class CompanyStructurePositionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Seniority { get; set; } = "Not specified";
    public int Headcount { get; set; } = 1;
    public string ReportsTo { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

internal sealed class BackendSaveCompanyStructureRequest
{
    public int ActorUserId { get; set; }
    public List<SaveCompanyStructureDepartmentInput> Departments { get; set; } = new();
}
