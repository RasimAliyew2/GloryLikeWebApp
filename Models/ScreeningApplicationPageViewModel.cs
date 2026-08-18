namespace GloryLikeWebApp.Models;

public sealed class ScreeningApplicationPageViewModel
{
    public int CandidateUserId { get; set; }
    public string DisplayName { get; set; } = "Candidate";
    public CandidateVacancyApiItem? Vacancy { get; set; }
    public ScreeningApplicationSubmissionModel Input { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;

    public string Initials
    {
        get
        {
            var parts = DisplayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();
            return parts.Count == 0
                ? "C"
                : string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
        }
    }
}

public sealed class ScreeningApplicationSubmissionModel
{
    public List<ScreeningApplicationAnswerInput> Answers { get; set; } = new();
}

public sealed class ScreeningApplicationAnswerInput
{
    public int QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public List<int> SelectedChoiceIds { get; set; } = new();
}
