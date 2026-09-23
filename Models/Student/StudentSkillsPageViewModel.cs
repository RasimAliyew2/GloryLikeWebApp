using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models.Student;

public sealed class StudentSkillsPageViewModel
{
    public List<SkillLookupItem> AvailableSkills { get; set; } = [];
    public Dictionary<int, string> AssessmentTypes { get; set; } = [];
    public bool TaxonomyAvailable { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public int AutoAssessmentSkillId { get; set; }
    public string AutoAssessmentSkillName { get; set; } = string.Empty;
}

public sealed class StudentSkillSelection
{
    [Range(1, int.MaxValue)]
    public int SkillId { get; set; }
}

public sealed class StudentSkillQuiz
{
    public Guid QuestionnaireId { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public List<StudentSkillQuizQuestion> Questions { get; set; } = [];
}

public sealed class StudentSkillQuizQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<StudentSkillQuizOption> Options { get; set; } = [];
}

public sealed class StudentSkillQuizOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class StudentSkillQuizResult
{
    public UserSkillInfo Skill { get; set; } = new();
    public int AnsweredQuestionCount { get; set; }
}
