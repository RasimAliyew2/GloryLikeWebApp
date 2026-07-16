using System.Diagnostics;
using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {

         if (User.Identity?.IsAuthenticated == false)
             return RedirectToAction("Account", "SignIn");



        var firstName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var surname = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var userName = User.FindFirstValue("username") ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = string.IsNullOrWhiteSpace(userName) ? "Candidate" : userName;

        var model = new CandidateDashboardViewModel
        {
            DisplayName = displayName,
            UserName = userName,
            Email = email,

            // These dashboard values are UI seed data for now.
            // Replace them later with real score/profile APIs.
            OverallScore = 76,
            ProfileCompletion = 76,
            StrongestRole = "Product Designer",
            StrongestRoleSubtitle = "Your highest readiness target",

            Stats =
            {
                new DashboardStatItem { Label = "Match", Value = "89", Caption = "Role fit" },
                new DashboardStatItem { Label = "Trust", Value = "92", Caption = "Evidence quality" },
                new DashboardStatItem { Label = "Level", Value = "Verified", Caption = "Profile status" },
                new DashboardStatItem { Label = "Strength", Value = "76%", Caption = "Overall score" }
            },

            Applications =
            {
                new DashboardApplicationItem
                {
                    Company = "Northstar",
                    Role = "Senior Product Designer",
                    Status = "Interview",
                    StatusClass = "interview",
                    UpdatedText = "Updated 2h ago"
                },
                new DashboardApplicationItem
                {
                    Company = "Vertex Labs",
                    Role = "Product Designer",
                    Status = "In review",
                    StatusClass = "review",
                    UpdatedText = "Updated yesterday"
                }
            },

            RecommendedJobs =
            {
                new DashboardJobItem
                {
                    Company = "Lumon",
                    CompanyInitials = "LU",
                    Role = "Senior Product Designer",
                    Meta = "Remote · Full-time",
                    MatchScore = 94,
                    Tags = { "Figma", "Design Systems", "Research" }
                },
                new DashboardJobItem
                {
                    Company = "Orbit",
                    CompanyInitials = "OR",
                    Role = "Product Designer",
                    Meta = "Hybrid · Full-time",
                    MatchScore = 88,
                    Tags = { "Product", "UX", "Prototyping" }
                },
                new DashboardJobItem
                {
                    Company = "Mosaic",
                    CompanyInitials = "MO",
                    Role = "UX Designer",
                    Meta = "Remote · Full-time",
                    MatchScore = 82,
                    Tags = { "Research", "Figma", "Accessibility" }
                }
            },

            Skills =
            {
                new DashboardSkillItem
                {
                    Name = "Figma",
                    Category = "Design",
                    Score = 91,
                    Knowledge = 95,
                    Experience = 88,
                    Status = "Verified",
                    StatusClass = "verified"
                },
                new DashboardSkillItem
                {
                    Name = "Product Strategy",
                    Category = "Product",
                    Score = 84,
                    Knowledge = 80,
                    Experience = 87,
                    Status = "Confirmed",
                    StatusClass = "confirmed"
                },
                new DashboardSkillItem
                {
                    Name = "UX Research",
                    Category = "Design",
                    Score = 73,
                    Knowledge = 78,
                    Experience = 68,
                    Status = "In review",
                    StatusClass = "review"
                }
            }
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
