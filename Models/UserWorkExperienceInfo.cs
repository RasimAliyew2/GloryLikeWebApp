namespace GloryLikeWebApp.Models;

public class UserWorkExperienceInfo
{
    public string CompanyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string StartYear { get; set; } = string.Empty;
    public string EndYear { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    // Mobile App-dakı köhnə property adları ilə compatibility.
    public string Position
    {
        get => PositionName;
        set => PositionName = value;
    }

    public string From
    {
        get => StartYear;
        set => StartYear = value;
    }

    public string Ending
    {
        get => EndYear;
        set => EndYear = value;
    }

    public string PeriodText
    {
        get
        {
            var start = string.IsNullOrWhiteSpace(StartYear) ? "Unknown" : StartYear.Trim();
            var end = string.IsNullOrWhiteSpace(EndYear) ? "Present" : EndYear.Trim();
            return $"{start} – {end}";
        }
    }

    public string CompanyInitial =>
        string.IsNullOrWhiteSpace(CompanyName)
            ? "?"
            : CompanyName.Trim()[0].ToString().ToUpperInvariant();
}
