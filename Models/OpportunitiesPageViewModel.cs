namespace GloryLikeWebApp.Models;

public sealed class OpportunitiesPageViewModel
{
    public int UserId { get; set; }

    public string DisplayName { get; set; } = "Candidate";
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string CurrentJobName { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;

    public List<OpportunityItem> Opportunities { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public string EmptyMessage { get; set; } =
        "Your current Job üçün SQL-də uyğun JobOffer tapılmadı.";

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasCurrentJob =>
        !string.IsNullOrWhiteSpace(CurrentJobName);

    public bool HasOpportunities =>
        Opportunities.Count > 0;

    public bool HasNoOpportunities =>
        !HasOpportunities && !HasError;

    public string RolesCountText =>
        Opportunities.Count == 0
            ? "No matching roles"
            : Opportunities.Count == 1
                ? "1 role for you"
                : $"{Opportunities.Count} roles for you";

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName)
                ? UserName
                : DisplayName;

            var parts = source
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();

            if (parts.Count == 0)
                return "C";

            return string.Concat(
                parts.Select(
                    part => char.ToUpperInvariant(part[0])));
        }
    }
}
