using System.ComponentModel.DataAnnotations;
namespace GloryLikeWebApp.Models.Employer;
public sealed class AutomationConditionInput
{
    public string Field { get; set; } = "Age";
    public string Operator { get; set; } = "GreaterThan";
    public decimal Value { get; set; }
}
public sealed class AutomationRuleInput
{
    [Required] public string EventType { get; set; } = string.Empty;
    public string? TargetStageName { get; set; } = string.Empty;
    [MaxLength(10)] public List<AutomationConditionInput> Conditions { get; set; } = [];
    public Guid LetterTemplateId { get; set; }
}
public class SaveCompanyAutomationInput
{
    [Required,StringLength(120)] public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public AutomationRuleInput Rule { get; set; } = new();
}
internal sealed class BackendSaveCompanyAutomationRequest : SaveCompanyAutomationInput
{
    public int ActorUserId { get; set; }
}
public sealed class CompanyAutomationItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public AutomationRuleInput Rule { get; set; } = new();
    public string LetterName { get; set; } = string.Empty;
    public bool LetterAvailable { get; set; }
}
public sealed class AutomationDeliveryItem
{
    public Guid Id { get; set; }
    public int VacancyId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
public sealed class CompanyAutomationApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public bool CanManageTemplates { get; set; }
    public List<CompanyAutomationItem> Templates { get; set; } = [];
    public List<CompanyTemplateItem> Letters { get; set; } = [];
    public List<string> StageNames { get; set; } = [];
    public List<AutomationDeliveryItem> Deliveries { get; set; } = [];
}
public sealed class CompanyAutomationsPageViewModel
{
    public string DisplayName { get; set; } = "Employer";
    public string Email { get; set; } = string.Empty;
    public string Initials => string.IsNullOrWhiteSpace(DisplayName) ? "EM" : DisplayName[..1].ToUpperInvariant();
    public string ErrorMessage { get; set; } = string.Empty;
    public CompanyAutomationApiResponse Data { get; set; } = new();
}
