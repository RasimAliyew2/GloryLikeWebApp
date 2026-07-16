using System.Security.Cryptography;
using System.Text;

namespace GloryLikeWebApp.Models;

public sealed class ProfilePageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Candidate";
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CurrentJobName { get; set; } = string.Empty;

    public List<UserSkillInfo> Skills { get; set; } = new();
    public List<UserWorkExperienceInfo> WorkExperiences { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSkills => Skills.Count > 0;
    public bool HasExperiences => WorkExperiences.Count > 0;
    public bool HasCurrentJob => !string.IsNullOrWhiteSpace(CurrentJobName);

    public int VerifiedSkillsCount => Skills.Count(skill =>
        skill.IsVerified ||
        string.Equals(
            skill.Status,
            "verified",
            StringComparison.OrdinalIgnoreCase));

    public int SelfDeclaredSkillsCount => Skills.Count(skill =>
        !skill.IsVerified &&
        !string.Equals(
            skill.Status,
            "verified",
            StringComparison.OrdinalIgnoreCase));

    public int AverageCredibility => Skills.Count == 0
        ? 0
        : RoundHalfUp(Skills.Average(skill => skill.CalculatedCredibilityScore));

    public int ProfileCompletion
    {
        get
        {
            var score = 0;

            if (!string.IsNullOrWhiteSpace(DisplayName) &&
                !DisplayName.Equals("Candidate", StringComparison.OrdinalIgnoreCase))
            {
                score += 20;
            }

            if (!string.IsNullOrWhiteSpace(UserName))
                score += 10;

            if (!string.IsNullOrWhiteSpace(Email))
                score += 10;

            if (!string.IsNullOrWhiteSpace(PhoneNumber))
                score += 10;

            if (HasCurrentJob)
                score += 15;

            if (HasSkills)
                score += 20;

            if (HasExperiences)
                score += 10;

            if (VerifiedSkillsCount > 0)
                score += 5;

            return Math.Clamp(score, 0, 100);
        }
    }

    public string Initials
    {
        get
        {
            var parts = DisplayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => part[0].ToString().ToUpperInvariant())
                .ToArray();

            if (parts.Length > 0)
                return string.Join(string.Empty, parts);

            return string.IsNullOrWhiteSpace(UserName)
                ? "U"
                : UserName.Trim()[0].ToString().ToUpperInvariant();
        }
    }

    // Stable visual accent without storing user-specific UI state.
    public int AvatarAccentIndex
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(UserName)
                ? DisplayName
                : UserName;

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source ?? string.Empty));
            return bytes[0] % 4;
        }
    }

    private static int RoundHalfUp(double value)
    {
        return (int)Math.Floor(Math.Clamp(value, 0, 100) + 0.5d);
    }
}
