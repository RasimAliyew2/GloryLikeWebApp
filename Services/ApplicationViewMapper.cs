using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public static class ApplicationViewMapper
{
    public static CandidateApplicationViewItem Map(
        CandidateApplicationApiItem application)
    {
        return new CandidateApplicationViewItem
        {
            ApplicationId = application.ApplicationId,
            VacancyId = application.VacancyId,
            CompanyOwnerUserId = application.CompanyOwnerUserId,
            PlatformVacancyId = application.PlatformVacancyId,
            CompanyName = string.IsNullOrWhiteSpace(application.CompanyName)
                ? "Employer"
                : application.CompanyName.Trim(),
            RoleTitle = string.IsNullOrWhiteSpace(application.RoleTitle)
                ? string.IsNullOrWhiteSpace(application.PositionName)
                    ? $"Vacancy #{application.VacancyId}"
                    : application.PositionName.Trim()
                : application.RoleTitle.Trim(),
            LocationName = application.LocationName ?? string.Empty,
            EmploymentType = application.EmploymentType ?? string.Empty,
            JobFamilyName = application.JobFamilyName ?? string.Empty,
            SeniorityName = application.SeniorityName ?? string.Empty,
            JobDescription = application.JobDescription ?? string.Empty,
            MinSalary = application.MinSalary,
            MaxSalary = application.MaxSalary,
            Currency = application.Currency ?? string.Empty,
            HideSalary = application.HideSalary,
            ApplicationDeadline = application.ApplicationDeadline,
            VacancyStatus = application.VacancyStatus ?? string.Empty,
            ApplicationStatus = application.ApplicationStatus ?? string.Empty,
            FunnelStageName = string.IsNullOrWhiteSpace(application.FunnelStageName)
                ? "Applied"
                : application.FunnelStageName.Trim(),
            FunnelStageIndex = application.FunnelStageIndex,
            FunnelStageCount = application.FunnelStageCount,
            AppliedAtUtc = application.AppliedAtUtc,
            FunnelStageUpdatedAtUtc = application.FunnelStageUpdatedAtUtc,
            HiredAtUtc = application.HiredAtUtc,
            Skills = (application.Skills ?? [])
                .Where(skill => !string.IsNullOrWhiteSpace(skill.SkillName))
                .Select(skill => new CandidateApplicationSkillItem
                {
                    SkillId = skill.SkillId,
                    SkillName = skill.SkillName.Trim(),
                    Weight = Math.Max(skill.Weight, 0),
                    RequirementType = skill.RequirementType ?? string.Empty
                })
                .ToList()
        };
    }
}
