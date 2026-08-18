using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerCompanyController : Controller
{
    private readonly ICompanyTeamApiService _companyTeamApiService;
    private readonly ICompanyProfileApiService _companyProfileApiService;
    private readonly ICompanyHiringPlanApiService _companyHiringPlanApiService;
    private readonly ISkillAndJobApiService _skillAndJobApiService;
    private readonly ILogger<EmployerCompanyController> _logger;

    public EmployerCompanyController(
        ICompanyTeamApiService companyTeamApiService,
        ICompanyProfileApiService companyProfileApiService,
        ICompanyHiringPlanApiService companyHiringPlanApiService,
        ISkillAndJobApiService skillAndJobApiService,
        ILogger<EmployerCompanyController> logger)
    {
        _companyTeamApiService = companyTeamApiService;
        _companyProfileApiService = companyProfileApiService;
        _companyHiringPlanApiService = companyHiringPlanApiService;
        _skillAndJobApiService = skillAndJobApiService;
        _logger = logger;
    }

    [HttpGet("/Employer/Company/HiringPlan")]
    public async Task<IActionResult> HiringPlan(CancellationToken cancellationToken)
    {
        var model = new CompanyHiringPlanPageViewModel
        {
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };

        if (!TryGetEmployerUserId(out var actorUserId))
        {
            model.ErrorMessage = "Employer sign in is required.";
            return View("HiringPlan", model);
        }

        model.UserId = actorUserId;
        var taxonomyTask = _skillAndJobApiService.GetJobFamiliesAsync(cancellationToken);
        var planTask = _companyHiringPlanApiService.GetAsync(actorUserId, cancellationToken);
        await Task.WhenAll(taxonomyTask, planTask);

        var taxonomy = await taxonomyTask;
        var plans = await planTask;

        if (taxonomy.Success)
        {
            model.JobFamilies = taxonomy.JobFamilies
                .Where(item => item.Id > 0)
                .OrderBy(item => item.JobName)
                .ToList();
        }

        if (plans.Success && plans.Data is not null)
        {
            model.Plans = plans.Data.Plans;
        }

        var errors = new[]
        {
            taxonomy.Success ? string.Empty : taxonomy.Message,
            plans.Success ? string.Empty : plans.Message
        }.Where(item => !string.IsNullOrWhiteSpace(item));
        model.ErrorMessage = string.Join(" ", errors);

        return View("HiringPlan", model);
    }

    [HttpPost("/Employer/Company/HiringPlan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHiringPlan(
        SaveCompanyHiringPlanInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized(new { success = false, message = "Employer sign in is required." });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstModelError() });

        var result = await _companyHiringPlanApiService.CreateAsync(
            actorUserId,
            input,
            cancellationToken);

        return result.Success
            ? Ok(new { success = true, message = result.Message, plan = result.Data?.Plan })
            : BadRequest(new { success = false, message = result.Message });
    }

    [HttpPost("/Employer/Company/HiringPlan/{planId:int}/Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateHiringPlan(
        int planId,
        SaveCompanyHiringPlanInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized(new { success = false, message = "Employer sign in is required." });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstModelError() });

        var result = await _companyHiringPlanApiService.UpdateAsync(
            actorUserId,
            planId,
            input,
            cancellationToken);

        return result.Success
            ? Ok(new { success = true, message = result.Message, plan = result.Data?.Plan })
            : BadRequest(new { success = false, message = result.Message });
    }

    [HttpPost("/Employer/Company/HiringPlan/{planId:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHiringPlan(
        int planId,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized(new { success = false, message = "Employer sign in is required." });

        var result = await _companyHiringPlanApiService.DeleteAsync(
            actorUserId,
            planId,
            cancellationToken);

        return result.Success
            ? Ok(new { success = true, message = result.Message })
            : BadRequest(new { success = false, message = result.Message });
    }

    [HttpGet("/Employer/Company")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = int.TryParse(userIdValue, out var userId);

        var model = new CompanyProfilePageViewModel
        {
            UserId = userId,
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };

        if (userId <= 0)
        {
            model.ErrorMessage =
                "Login məlumatında employer user ID tapılmadı.";
            return View("CompanyProfile", model);
        }

        var result = await _companyProfileApiService.GetAsync(
            userId,
            cancellationToken);

        if (!result.Success
            || result.Data?.Profile is null)
        {
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Company profile yüklənmədi."
                : result.Message;
        }
        else
        {
            model.Profile = result.Data.Profile;
            model.Profile.Benefits ??= [];
        }

        return View("CompanyProfile", model);
    }

    [HttpPost("/Employer/Company/Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(
        CompanyProfileInput profile,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Json(new
            {
                success = false,
                message = "Login məlumatı tapılmadı. Yenidən sign in edin."
            });
        }

        profile.Benefits ??= [];

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new
            {
                success = false,
                message = ModelState.Values
                    .SelectMany(item => item.Errors)
                    .Select(item => item.ErrorMessage)
                    .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))
                    ?? "Please check the company profile fields."
            });
        }

        var result = await _companyProfileApiService.SaveAsync(
            actorUserId,
            profile,
            cancellationToken);

        if (!result.Success)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new
            {
                success = false,
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? "Company profile saxlanmadı."
                    : result.Message
            });
        }

        return Json(new
        {
            success = true,
            message = string.IsNullOrWhiteSpace(result.Message)
                ? "Company profile bütün team üçün yeniləndi."
                : result.Message
        });
    }

    [HttpGet("/Employer/Company/Team")]
    public async Task<IActionResult> Team(
        CancellationToken cancellationToken)
    {
        var model = new CompanyTeamPageViewModel
        {
            DisplayName = GetDisplayName(),
            Email =
                User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty
        };

        if (!TryGetEmployerUserId(out var ownerUserId))
        {
            model.ErrorMessage =
                "Login məlumatında employer user ID tapılmadı. Yenidən sign in edin.";

            return View("Team", model);
        }

        model.UserId = ownerUserId;

        var result =
            await _companyTeamApiService.GetTeamAsync(
                ownerUserId,
                cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage =
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Company team yüklənmədi."
                    : result.Message;

            _logger.LogWarning(
                "Employer {OwnerUserId} üçün Company Team yüklənmədi: {Message}",
                ownerUserId,
                model.ErrorMessage);

            return View("Team", model);
        }

        model.CompanyName = result.Data.CompanyName;
        model.CanManageTeam = result.Data.CanManageTeam;
        model.Members = result.Data.Members
            .Select(ToTeamMemberViewModel)
            .OrderBy(item => RoleOrder(item.Role))
            .ThenBy(item => item.IsInvited)
            .ThenBy(item => item.DisplayName)
            .ToList();

        return View("Team", model);
    }

    [HttpPost("/Employer/Company/Team/Invite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(
        InviteCompanyTeamMemberViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var ownerUserId))
        {
            Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return Json(new
            {
                success = false,
                message =
                    "Login məlumatı tapılmadı. Yenidən sign in edin."
            });
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode =
                StatusCodes.Status400BadRequest;

            var message = ModelState.Values
                .SelectMany(item => item.Errors)
                .Select(item => item.ErrorMessage)
                .FirstOrDefault(
                    item => !string.IsNullOrWhiteSpace(item))
                ?? "Email və role məlumatlarını yoxlayın.";

            return Json(new
            {
                success = false,
                message
            });
        }

        var result =
            await _companyTeamApiService.InviteAsync(
                ownerUserId,
                model,
                cancellationToken);

        if (!result.Success)
        {
            Response.StatusCode =
                StatusCodes.Status400BadRequest;

            return Json(new
            {
                success = false,
                message =
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Invitation göndərilmədi."
                        : result.Message
            });
        }

        return Json(new
        {
            success = true,
            message =
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Invitation email göndərildi."
                    : result.Message
        });
    }

    [HttpPost("/Employer/Company/Team/Remove/{invitationId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
        {
            Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return Json(new
            {
                success = false,
                message =
                    "Login məlumatı tapılmadı. Yenidən sign in edin."
            });
        }

        var result =
            await _companyTeamApiService.RemoveMemberAsync(
                actorUserId,
                invitationId,
                cancellationToken);

        if (!result.Success)
        {
            Response.StatusCode =
                StatusCodes.Status400BadRequest;

            return Json(new
            {
                success = false,
                message =
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Team üzvü silinmədi."
                        : result.Message
            });
        }

        return Json(new
        {
            success = true,
            message =
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Team üzvü silindi."
                    : result.Message
        });
    }

    private bool TryGetEmployerUserId(out int userId)
    {
        return int.TryParse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier),
                out userId)
            && userId > 0;
    }

    private string FirstModelError() => ModelState.Values
        .SelectMany(item => item.Errors)
        .Select(item => item.ErrorMessage)
        .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))
        ?? "Please check the hiring plan fields.";

    private static CompanyTeamMemberViewModel
        ToTeamMemberViewModel(
            CompanyTeamMemberApiItem item)
    {
        return new CompanyTeamMemberViewModel
        {
            InvitationId = item.InvitationId,
            UserId = item.UserId,
            DisplayName = item.DisplayName,
            Email = item.Email,
            Role = item.Role,
            Status = item.Status,
            InvitedAtUtc = item.InvitedAtUtc,
            AcceptedAtUtc = item.AcceptedAtUtc,
            IsFounder = item.IsFounder
        };
    }

    private static int RoleOrder(string role)
    {
        return role switch
        {
            "Admin" => 0,
            "HR Admin" => 1,
            "Hiring Manager" => 2,
            "Recruiter" => 3,
            _ => 4
        };
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
}
