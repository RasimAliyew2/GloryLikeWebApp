using System.ComponentModel.DataAnnotations;

namespace GloryLikeWebApp.Models;

public sealed class StartSkillAssessmentRequest
{
    public int SkillId { get; set; }

    [Required]
    [MaxLength(150)]
    public string SkillName { get; set; } = string.Empty;

    [RegularExpression("^(az|ru|en)$")]
    public string Language { get; set; } = "az";
}

public sealed class CompleteSkillAssessmentRequest
{
    public int SkillId { get; set; }

    [Required]
    [MaxLength(150)]
    public string SkillName { get; set; } = string.Empty;

    [Required]
    public Guid QuestionnaireId { get; set; }

    [Required]
    [MinLength(1)]
    public List<SkillAssessmentAnswer> Answers { get; set; } = new();
}

public sealed class SkillAssessmentAnswer
{
    [Required]
    public string QuestionId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<string> SelectedOptionIds { get; set; } = new();
}

public sealed class GenerateSkillQuestionnaireRequest
{
    public string Skill { get; set; } = string.Empty;
    public string SkillComplexity { get; set; } = "medium";
    public string Seniority { get; set; } = "middle";
    public string Language { get; set; } = "az";
}

public sealed class SkillQuestionnaireResponse
{
    public Guid QuestionnaireId { get; set; }
    public string Skill { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public string SkillComplexity { get; set; } = string.Empty;
    public List<QuestionnaireQuestion> Questions { get; set; } = new();
    public QuestionnaireScoring Scoring { get; set; } = new();
}

public sealed class QuestionnaireQuestion
{
    public string Id { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Dimension { get; set; } = string.Empty;
    public bool HiddenByDefault { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "single";
    public List<QuestionnaireOption> Options { get; set; } = new();
    public List<QuestionnaireBranchingRule> Branching { get; set; } = new();
}

public sealed class QuestionnaireOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public QuestionnaireOptionWeights Weights { get; set; } = new();
}

public sealed class QuestionnaireOptionWeights
{
    public int Complexity { get; set; }
    public int Ownership { get; set; }
    public int Depth { get; set; }
}

public sealed class QuestionnaireBranchingRule
{
    public string IfOption { get; set; } = string.Empty;
    public string RevealQuestionId { get; set; } = string.Empty;
}

public sealed class QuestionnaireScoring
{
    public int MaxComplexity { get; set; }
    public int MaxOwnership { get; set; }
    public int MaxDepth { get; set; }
}

public sealed class SubmitSkillDepthAssessmentRequest
{
    public Guid QuestionnaireId { get; set; }
    public List<SkillAssessmentAnswer> Answers { get; set; } = new();
}

public sealed class SkillDepthAssessmentResult
{
    public Guid QuestionnaireId { get; set; }
    public string Skill { get; set; } = string.Empty;
    public double ComplexityRatio { get; set; }
    public double OwnershipRatio { get; set; }
    public double DepthRatio { get; set; }
    public int DepthScore { get; set; }
    public string TaskComplexity { get; set; } = string.Empty;
    public string OwnershipLevel { get; set; } = string.Empty;
    public string DepthTier { get; set; } = string.Empty;
    public int AnsweredQuestionCount { get; set; }
}
