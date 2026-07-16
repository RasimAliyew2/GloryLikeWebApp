namespace GloryLikeWebApp.Models;

public class UserSkillInfo
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;

    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;

    public int SeniorityId { get; set; }
    public string SeniorityName { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;

    public string SkillComplexity { get; set; } = "medium";

    // verified | self_declared | absent
    public string Status { get; set; } = "self_declared";
    public bool IsVerified { get; set; }

    public double KnowledgeScore { get; set; }
    public double ExperienceScore { get; set; }
    public double DepthScore { get; set; }
    public double CredibilityScore { get; set; }

    public string TaskComplexity { get; set; } = string.Empty;
    public string OwnershipLevel { get; set; } = string.Empty;
    public string DepthTier { get; set; } = string.Empty;

    public double ContextScore { get; set; }
    public double ComplexityScore { get; set; }
    public double OwnershipScore { get; set; }
    public double ResultScore { get; set; }

    public double CalculatedCredibilityScore =>
        CredibilityScore > 0
            ? Math.Clamp(CredibilityScore, 0, 100)
            : Math.Clamp((KnowledgeScore * 0.45d) + (ExperienceScore * 0.55d), 0, 100);

    public double Signal
    {
        get
        {
            var normalizedStatus = (Status ?? string.Empty).Trim().ToLowerInvariant();
            var credibility = CalculatedCredibilityScore;

            if (IsVerified || normalizedStatus == "verified")
                return credibility;

            if (normalizedStatus == "self_declared" || string.IsNullOrWhiteSpace(normalizedStatus))
                return Math.Min(credibility, 40);

            return 0;
        }
    }

    public int KnowledgeDisplay => RoundHalfUp(KnowledgeScore);
    public int ExperienceDisplay => RoundHalfUp(ExperienceScore);
    public int DepthDisplay => RoundHalfUp(DepthScore);
    public int CredibilityDisplay => RoundHalfUp(CalculatedCredibilityScore);

    public string StatusText =>
        IsVerified || string.Equals(Status, "verified", StringComparison.OrdinalIgnoreCase)
            ? "Verified"
            : string.Equals(Status, "absent", StringComparison.OrdinalIgnoreCase)
                ? "Absent"
                : "Self-declared";

    public string StatusClass =>
        IsVerified || string.Equals(Status, "verified", StringComparison.OrdinalIgnoreCase)
            ? "verified"
            : string.Equals(Status, "absent", StringComparison.OrdinalIgnoreCase)
                ? "absent"
                : "self-declared";

    public string UsedInText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PositionName))
                return $"Used in: {PositionName}";

            if (!string.IsNullOrWhiteSpace(JobFamilyName))
                return $"Used in: {JobFamilyName}";

            return "Not linked to experience";
        }
    }

    private static int RoundHalfUp(double value)
    {
        return (int)Math.Floor(Math.Clamp(value, 0, 100) + 0.5d);
    }
}
