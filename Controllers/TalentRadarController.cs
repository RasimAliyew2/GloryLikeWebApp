using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class TalentRadarController : Controller
{
    private readonly ITalentRadarApiService _talentRadarApiService;
    private readonly ILogger<TalentRadarController> _logger;

    public TalentRadarController(
        ITalentRadarApiService talentRadarApiService,
        ILogger<TalentRadarController> logger)
    {
        _talentRadarApiService = talentRadarApiService;
        _logger = logger;
    }

    [HttpGet("/Employer/TalentRadar")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model = new TalentRadarPageViewModel
        {
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty
        };

        if (!TryGetEmployerUserId(out var employerUserId))
        {
            model.ErrorMessage =
                "Login məlumatında employer user ID tapılmadı. Yenidən sign in edin.";
            return View("TalentRadar", model);
        }

        var result = await _talentRadarApiService.GetAsync(
            employerUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Talent Radar yüklənmədi."
                : result.Message;

            _logger.LogWarning(
                "Employer {EmployerUserId} üçün Talent Radar yüklənmədi: {Message}",
                employerUserId,
                model.ErrorMessage);

            return View("TalentRadar", model);
        }

        var data = result.Data;

        model.StatusMessage = data.Message;
        model.TotalVacancies = data.TotalVacancies;
        model.ScoredVacancies = data.ScoredVacancies;
        model.Candidates = data.Candidates
            .Select(candidate => new TalentRadarCandidateViewModel
            {
                UserId = candidate.UserId,
                Name = candidate.Name,
                CurrentRole = candidate.CurrentRole,
                JobFamilyName = candidate.JobFamilyName,
                BestVacancyId = candidate.BestVacancyId,
                PlatformVacancyId = candidate.PlatformVacancyId,
                TargetRoleTitle = candidate.TargetRoleTitle,
                RoleReadiness = RoundHalfUp(
                    candidate.RoleReadiness),
                MatchedVacancyCount = candidate.MatchedVacancyCount,
                MatchedSkillsCount = candidate.MatchedSkillsCount,
                TemplateSkillsCount = candidate.TemplateSkillsCount,
                Skills = candidate.Skills
                    .Select(skill => new TalentRadarSkillViewModel
                    {
                        SkillId = skill.SkillId,
                        SkillName = skill.SkillName,
                        Score = Math.Clamp(skill.Score, 0, 100),
                        IsVerified = skill.IsVerified
                    })
                    .ToList()
            })
            .OrderByDescending(candidate => candidate.RoleReadiness)
            .ThenBy(candidate => candidate.Name)
            .ToList();

        return View("TalentRadar", model);
    }

    private bool TryGetEmployerUserId(out int userId)
    {
        return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId)
            && userId > 0;
    }

    private string GetDisplayName()
    {
        var firstName = User.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        var surname = User.FindFirstValue(ClaimTypes.Surname)
            ?? string.Empty;
        var userName = User.FindFirstValue("username")
            ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(displayName)
            ? string.IsNullOrWhiteSpace(userName)
                ? "Employer"
                : userName
            : displayName;
    }

    private static int RoundHalfUp(double value)
    {
        return (int)Math.Floor(
            Math.Clamp(value, 0d, 100d) + 0.5d);
    }
}
