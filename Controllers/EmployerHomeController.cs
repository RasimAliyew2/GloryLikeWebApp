using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerHomeController : Controller
{
    [HttpGet("/EmployerHome")]
    public IActionResult EmployerHome()
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
            },

            Candidates =
            {
                new EmployerCandidateItem
                {
                    Name = "Alex Morgan",
                    CurrentCompany = "Telecom",
                    CurrentRole = "Data Analytics",
                    TrustScore = 82,
                    MatchScore = 91,
                    Signals =
                    {
                        "SQL High",
                        "Segmentation"
                    }
                },
                new EmployerCandidateItem
                {
                    Name = "Jamie Lee",
                    CurrentCompany = "Fintech",
                    CurrentRole = "Product Analyst",
                    TrustScore = 79,
                    MatchScore = 87,
                    Signals =
                    {
                        "Research",
                        "Python"
                    }
                },
                new EmployerCandidateItem
                {
                    Name = "Taylor Brooks",
                    CurrentCompany = "Retail",
                    CurrentRole = "People Analytics",
                    TrustScore = 76,
                    MatchScore = 84,
                    Signals =
                    {
                        "HR Analytics",
                        "SQL"
                    }
                }
            }
        };

        return View("EmployerHome", model);
    }
}
