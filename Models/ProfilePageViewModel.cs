using System.Security.Cryptography;
using System.Text;

namespace GloryLikeWebApp.Models;

public sealed class ProfilePageViewModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = "Candidate";
    public string AccountType { get; set; } = "candidate";
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CurrentJobName { get; set; } = string.Empty;
    public UserPersonalProfileInput Personal { get; set; } = new();
    public bool IsEditMode { get; set; }
    public string? SuccessMessage { get; set; }

    public List<UserSkillInfo> Skills { get; set; } = new();
    public List<UserWorkExperienceInfo> WorkExperiences { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool HasSkills => Skills.Count > 0;
    public bool HasExperiences => WorkExperiences.Count > 0;
    public bool HasCurrentJob => !string.IsNullOrWhiteSpace(CurrentJobName);
    public bool IsEmployer => string.Equals(
        AccountType,
        "employer",
        StringComparison.OrdinalIgnoreCase);
    public string AccountLabel => IsEmployer ? "Employer" : "Candidate";
    public string ProfileKindLabel => IsEmployer
        ? "EMPLOYER PROFILE"
        : "CANDIDATE PROFILE";
    public string HomeUrl => "/Portal/Home";
    public bool HasProfileImage => !string.IsNullOrWhiteSpace(
        Personal.ProfileImageDataUrl);
    public bool HasBirthDate => Personal.BirthDate.HasValue;
    public bool HasAbout => !string.IsNullOrWhiteSpace(Personal.About);
    public string BirthDateDisplay => Personal.BirthDate?.ToString("dd MMMM yyyy")
        ?? "Not added";

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

            if (!string.IsNullOrWhiteSpace(Personal.FirstName))
                score += 15;

            if (!string.IsNullOrWhiteSpace(Personal.LastName))
                score += 15;

            if (!string.IsNullOrWhiteSpace(UserName))
                score += 10;

            if (!string.IsNullOrWhiteSpace(Email))
                score += 10;

            if (HasBirthDate)
                score += 10;

            if (HasAbout)
                score += 15;

            if (HasProfileImage)
                score += 10;

            if (IsEmployer)
                return Math.Clamp(score + 15, 0, 100);

            if (HasCurrentJob)
                score += 5;

            if (HasSkills)
                score += 5;

            if (HasExperiences)
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
