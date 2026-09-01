using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerHomeController : Controller
{
    private readonly ITalentRadarApiService _talentRadarApiService;
    private readonly ILogger<EmployerHomeController> _logger;

    public EmployerHomeController(
        ITalentRadarApiService talentRadarApiService,
        ILogger<EmployerHomeController> logger)
    {
        _talentRadarApiService = talentRadarApiService;
        _logger = logger;
    }

    [HttpGet("/EmployerHome")]
    public async Task<IActionResult> EmployerHome(
        CancellationToken cancellationToken)
    {
        var firstName =
            User.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;

        var surname =
            User.FindFirstValue(ClaimTypes.Surname)
            ?? string.Empty;

        var userName =
            User.FindFirstValue("username")
            ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(
                    value => !string.IsNullOrWhiteSpace(value)));

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = string.IsNullOrWhiteSpace(userName)
                ? "Employer"
                : userName;
        }

        var model = new EmployerHomeViewModel
        {
            DisplayName = displayName,
            Email =
                User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty,

            Stats =
            {
                new EmployerDashboardStatItem
                {
                    Label = "OPEN ROLES",
                    Value = "11",
                    Icon = "▣",
                    AccentClass = "purple"
                },
                new EmployerDashboardStatItem
                {
                    Label = "IN THE FUNNEL",
                    Value = "40",
                    Icon = "◎",
                    AccentClass = "blue"
                },
                new EmployerDashboardStatItem
                {
                    Label = "INTERVIEW",
                    Value = "6",
                    Icon = "▦",
                    AccentClass = "orange"
                },
                new EmployerDashboardStatItem
                {
                    Label = "OFFER",
                    Value = "1",
                    Icon = "♢",
                    AccentClass = "green"
                }
            },

            Insights =
            {
                new EmployerInsightItem
                {
                    Label = "Medium Skill Match",
                    Value = "78%",
                    Caption = "+3% per month",
                    Icon = "◎"
                },
                new EmployerInsightItem
                {
                    Label = "Main Skill Gap",
                    Value = "Python",
                    Caption = "4 vacancies without coverage",
                    Icon = "△"
                },
                new EmployerInsightItem
                {
                    Label = "High Trust Candidates",
                    Value = "12",
                    Caption = "Trust Score > 75",
                    Icon = "⬡"
                }
            }
        };

        if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var employerUserId)
            || employerUserId <= 0)
        {
            model.CandidateEmptyMessage =
                "Candidate matches could not be loaded. Please sign in again.";
            return View("EmployerHome", model);
        }

        var result = await _talentRadarApiService.GetAsync(
            employerUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.CandidateEmptyMessage =
                "Candidate matches could not be loaded right now.";

            _logger.LogWarning(
                "Employer dashboard candidate matches failed for user {EmployerUserId}: {Message}",
                employerUserId,
                result.Message);

            return View("EmployerHome", model);
        }

        var data = result.Data;

        model.Candidates = data.Candidates
            .OrderByDescending(candidate => candidate.RoleReadiness)
            .ThenBy(candidate => candidate.Name)
            .Take(4)
            .Select(candidate => new EmployerCandidateItem
            {
                UserId = candidate.UserId,
                Name = candidate.Name,
                CurrentCompany = candidate.JobFamilyName,
                CurrentRole = candidate.CurrentRole,
                TrustScore = CalculateTrustScore(candidate),
                MatchScore = RoundHalfUp(candidate.RoleReadiness),
                Signals = candidate.Skills
                    .Where(skill => !string.IsNullOrWhiteSpace(skill.SkillName))
                    .OrderByDescending(skill => skill.IsVerified)
                    .ThenByDescending(skill => skill.Score)
                    .Select(skill => skill.SkillName.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(2)
                    .ToList()
            })
            .ToList();

        if (model.Candidates.Count == 0)
        {
            model.CandidateEmptyMessage = data.TotalVacancies == 0
                ? "No active vacancies. Activate a vacancy to see candidate matches."
                : "No candidates match your active vacancies yet.";
        }

        return View("EmployerHome", model);
    }

    private static int CalculateTrustScore(
        TalentRadarCandidateApiItem candidate)
    {
        var verifiedSkills = candidate.Skills
            .Where(skill => skill.IsVerified)
            .ToList();

        if (verifiedSkills.Count == 0)
            return 0;

        return RoundHalfUp(verifiedSkills.Average(
            skill => Math.Clamp(skill.Score, 0, 100)));
    }

    private static int RoundHalfUp(double value)
    {
        return (int)Math.Floor(
            Math.Clamp(value, 0d, 100d) + 0.5d);
    }
}
