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
    private readonly ICompanyStructureApiService _companyStructureApiService;
    private readonly ISkillAndJobApiService _skillAndJobApiService;
    private readonly ILogger<EmployerCompanyController> _logger;

    public EmployerCompanyController(
        ICompanyTeamApiService companyTeamApiService,
        ICompanyProfileApiService companyProfileApiService,
        ICompanyHiringPlanApiService companyHiringPlanApiService,
        ICompanyStructureApiService companyStructureApiService,
        ISkillAndJobApiService skillAndJobApiService,
        ILogger<EmployerCompanyController> logger)
    {
        _companyTeamApiService = companyTeamApiService;
        _companyProfileApiService = companyProfileApiService;
        _companyHiringPlanApiService = companyHiringPlanApiService;
        _companyStructureApiService = companyStructureApiService;
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
        var structureTask = _companyStructureApiService.GetAsync(actorUserId, cancellationToken);
        await Task.WhenAll(taxonomyTask, planTask, structureTask);

        var taxonomy = await taxonomyTask;
        var plans = await planTask;
        var structure = await structureTask;

        if (structure.Success && structure.Data is not null)
        {
            model.Departments = BuildHiringPlanStructureOptions(
                structure.Data.Departments);
        }

        if (taxonomy.Success)
        {
            model.Seniorities = taxonomy.JobFamilies
                .SelectMany(job => job.Positions)
                .SelectMany(position => position.Seniorities)
                .Where(seniority => seniority.Id > 0)
                .GroupBy(seniority => seniority.Id)
                .Select(group => group
                    .OrderBy(seniority => seniority.SortOrder)
                    .First())
                .OrderBy(seniority => seniority.SortOrder)
                .ThenBy(seniority => seniority.Name)
                .Select(seniority => new CompanyHiringPlanSeniorityOption
                {
                    Id = seniority.Id,
                    Name = seniority.Name,
                    SortOrder = seniority.SortOrder
                })
                .ToList();
        }

        if (plans.Success && plans.Data is not null)
        {
            model.Plans = plans.Data.Plans;
        }

        var errors = new[]
        {
            taxonomy.Success ? string.Empty : taxonomy.Message,
            plans.Success ? string.Empty : plans.Message,
            structure.Success ? string.Empty : structure.Message
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

    [HttpPost("/Employer/Company/HiringPlan/Import")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ImportHiringPlan(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized(new { success = false, message = "Employer sign in is required." });

        var fileError = ValidateExcelFile(file);
        if (!string.IsNullOrWhiteSpace(fileError))
            return BadRequest(new { success = false, message = fileError });

        await using var stream = file!.OpenReadStream();
        var result = await _companyHiringPlanApiService.ImportAsync(
            actorUserId,
            stream,
            file.FileName,
            cancellationToken);

        return result.Success
            ? Ok(new { success = true, message = result.Message })
            : BadRequest(new { success = false, message = result.Message });
    }

    [HttpGet("/Employer/Company/Structure")]
    public async Task<IActionResult> Structure(CancellationToken cancellationToken)
    {
        var model = new CompanyStructurePageViewModel
        {
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            ErrorMessage = TempData["CompanyStructureError"] as string ?? string.Empty
        };

        if (!TryGetEmployerUserId(out var actorUserId))
        {
            model.ErrorMessage = "Employer sign in is required.";
            return View("Structure", model);
        }

        model.UserId = actorUserId;
        var result = await _companyStructureApiService.GetAsync(
            actorUserId,
            cancellationToken);
        if (result.Success && result.Data is not null)
            model.Departments = result.Data.Departments;
        else if (string.IsNullOrWhiteSpace(model.ErrorMessage))
            model.ErrorMessage = result.Message;

        return View("Structure", model);
    }

    [HttpPost("/Employer/Company/Structure/Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveStructure(
        [FromBody] SaveCompanyStructureInput? input,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized(new { success = false, message = "Employer sign in is required." });

        if (input is null)
            return BadRequest(new { success = false, message = "Company structure data is required." });

        input.Departments ??= new();
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstModelError() });

        var result = await _companyStructureApiService.SaveAsync(
            actorUserId,
            input,
            cancellationToken);

        return result.Success
            ? Ok(new
            {
                success = true,
                message = result.Message,
                departments = result.Data?.Departments
            })
            : BadRequest(new { success = false, message = result.Message });
    }

    [HttpPost("/Employer/Company/Structure/Import")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ImportStructure(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized(new { success = false, message = "Employer sign in is required." });

        var fileError = ValidateExcelFile(file);
        if (!string.IsNullOrWhiteSpace(fileError))
            return BadRequest(new { success = false, message = fileError });

        await using var stream = file!.OpenReadStream();
        var result = await _companyStructureApiService.ImportAsync(
            actorUserId,
            stream,
            file.FileName,
            cancellationToken);

        return result.Success
            ? Ok(new { success = true, message = result.Message })
            : BadRequest(new { success = false, message = result.Message });
    }

    [HttpGet("/Employer/Company/Structure/Export")]
    public async Task<IActionResult> ExportStructure(
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized();

        var result = await _companyStructureApiService.ExportAsync(
            actorUserId,
            cancellationToken);
        if (!result.Success)
        {
            TempData["CompanyStructureError"] = result.Message;
            return RedirectToAction(nameof(Structure));
        }

        return File(
            result.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.FileName);
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
            model.CompanyOwnerUserId = result.Data.CompanyOwnerUserId;
            model.Profile = result.Data.Profile;
            model.Profile.Benefits ??= [];
            model.Profile.Locations ??= [];
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
        profile.Locations ??= [];

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
                : result.Message,
            companyOwnerUserId = result.Data?.CompanyOwnerUserId,
            profile = result.Data?.Profile
        });
    }

    [HttpPost("/Employer/Company/AboutPage/Ai")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomizeAboutPageWithAi(
        CompanyAboutAiInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized(new { success = false, message = "Employer sign in is required." });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstModelError() });

        var result = await _companyProfileApiService.CustomizeWithAiAsync(
            actorUserId,
            input,
            cancellationToken);

        var data = result.Data;
        return result.Success && data is not null
            ? Ok(new
            {
                success = true,
                allowed = data.Allowed,
                message = data.Message,
                html = data.Html,
                mode = data.Mode,
                changeSummary = data.ChangeSummary,
                changedSelectors = data.ChangedSelectors
            })
            : BadRequest(new
            {
                success = false,
                allowed = data?.Allowed ?? false,
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? "AI about page dizaynı hazırlanmadı."
                    : result.Message,
                html = data?.Html ?? input.CurrentHtml
            });
    }

    [HttpGet("/Employer/Company/Team")]
    public async Task<IActionResult> Team(
        string? tab,
        CancellationToken cancellationToken)
    {
        var model = new CompanyTeamPageViewModel
        {
            DisplayName = GetDisplayName(),
            Email =
                User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty,
            ActiveTab = NormalizeTeamTab(tab),
            SuccessMessage = TempData["TeamSuccess"]?.ToString() ?? string.Empty,
            ErrorMessage = TempData["TeamError"]?.ToString() ?? string.Empty
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
        model.CanManageRoles = result.Data.CanManageRoles;
        model.CanInvite = result.Data.CanInvite;
        model.ActorRole = result.Data.ActorRole;
        model.Members = result.Data.Members
            .Select(ToTeamMemberViewModel)
            .OrderBy(item => RoleOrder(item.Role))
            .ThenBy(item => item.IsInvited)
            .ThenBy(item => item.DisplayName)
            .ToList();
        model.Roles = result.Data.Roles
            .Select(ToAccessRoleViewModel)
            .ToList();
        model.History = result.Data.History
            .Select(ToAccessHistoryViewModel)
            .ToList();
        model.PermissionGroups = result.Data.PermissionGroups
            .Select(ToPermissionGroupViewModel)
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

    [HttpPost("/Employer/Company/Team/Role/{invitationId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMemberRole(
        Guid invitationId,
        UpdateCompanyTeamMemberRoleViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Login məlumatı tapılmadı. Yenidən sign in edin."
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Access level düzgün seçilməyib."
            });
        }

        var result = await _companyTeamApiService.UpdateMemberRoleAsync(
            actorUserId,
            invitationId,
            model.RoleId!.Value,
            cancellationToken);

        return result.Success
            ? Ok(new
            {
                success = true,
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? "Access level yeniləndi."
                    : result.Message
            })
            : BadRequest(new
            {
                success = false,
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? "Access level dəyişdirilmədi."
                    : result.Message
            });
    }

    [HttpGet("/Employer/Company/Team/Roles/New")]
    [HttpGet("/Employer/Company/Team/Roles/{roleId:guid}/Edit")]
    public async Task<IActionResult> RoleEditor(
        Guid? roleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return RedirectToAction("SignIn", "Account");

        var result = await _companyTeamApiService.GetTeamAsync(
            actorUserId,
            cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["TeamError"] = result.Message;
            return RedirectToAction(nameof(Team), new { tab = "roles" });
        }
        if (!result.Data.CanManageRoles)
            return Forbid();

        CompanyAccessRoleApiItem? existing = null;
        if (roleId.HasValue)
        {
            existing = result.Data.Roles.FirstOrDefault(item => item.Id == roleId.Value);
            if (existing is null)
                return NotFound();
            if (existing.IsSystem)
            {
                TempData["TeamError"] = "HR Admin sistem rolu dəyişdirilə bilməz.";
                return RedirectToAction(nameof(Team), new { tab = "roles" });
            }
        }

        var model = new CompanyRoleEditorPageViewModel
        {
            UserId = actorUserId,
            DisplayName = GetDisplayName(),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            CompanyName = result.Data.CompanyName,
            PermissionGroups = result.Data.PermissionGroups
                .Select(ToPermissionGroupViewModel)
                .ToList(),
            Role = existing is null
                ? new SaveCompanyAccessRoleViewModel()
                : new SaveCompanyAccessRoleViewModel
                {
                    RoleId = existing.Id,
                    Name = existing.Name,
                    Description = existing.Description,
                    Scope = existing.Scope,
                    PermissionKeys = existing.PermissionKeys ?? []
                }
        };
        return View("RoleEditor", model);
    }

    [HttpPost("/Employer/Company/Team/Roles/Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRole(
        SaveCompanyAccessRoleViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var actorUserId))
            return Unauthorized();

        model.PermissionKeys ??= [];
        if (!ModelState.IsValid)
        {
            var team = await _companyTeamApiService.GetTeamAsync(
                actorUserId,
                cancellationToken);
            var editor = new CompanyRoleEditorPageViewModel
            {
                UserId = actorUserId,
                DisplayName = GetDisplayName(),
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                CompanyName = team.Data?.CompanyName ?? string.Empty,
                ErrorMessage = FirstModelError(),
                Role = model,
                PermissionGroups = team.Data?.PermissionGroups
                    .Select(ToPermissionGroupViewModel)
                    .ToList() ?? []
            };
            return View("RoleEditor", editor);
        }

        var result = await _companyTeamApiService.SaveRoleAsync(
            actorUserId,
            model,
            cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var team = await _companyTeamApiService.GetTeamAsync(
                actorUserId,
                cancellationToken);
            return View("RoleEditor", new CompanyRoleEditorPageViewModel
            {
                UserId = actorUserId,
                DisplayName = GetDisplayName(),
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                CompanyName = team.Data?.CompanyName ?? string.Empty,
                ErrorMessage = result.Message,
                Role = model,
                PermissionGroups = team.Data?.PermissionGroups
                    .Select(ToPermissionGroupViewModel)
                    .ToList() ?? []
            });
        }

        TempData["TeamSuccess"] = model.RoleId.HasValue
            ? "Rol və access-lər yeniləndi."
            : "Yeni rol yaradıldı.";
        return RedirectToAction(nameof(Team), new { tab = "roles" });
    }

    private bool TryGetEmployerUserId(out int userId)
    {
        return int.TryParse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier),
                out userId)
            && userId > 0;
    }

    private static List<CompanyHiringPlanDepartmentOption> BuildHiringPlanStructureOptions(
        IEnumerable<CompanyStructureDepartmentItem> departments)
    {
        return departments
            .Select(department => new CompanyHiringPlanDepartmentOption
            {
                Name = department.Name,
                Positions = department.Divisions
                    .SelectMany(division => division.Positions)
                    .GroupBy(position => NormalizeName(position.Name))
                    .Select(group => new CompanyHiringPlanPositionOption
                    {
                        Name = group.First().Name
                    })
                    .OrderBy(position => position.Name)
                    .ToList()
            })
            .Where(department => department.Positions.Count > 0)
            .OrderBy(department => department.Name)
            .ToList();
    }

    private static string ValidateExcelFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "Select a non-empty .xlsx file.";
        if (file.Length > 5 * 1024 * 1024)
            return "Excel file cannot be larger than 5 MB.";
        if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Only .xlsx files are supported.";
        }

        return string.Empty;
    }

    private static string NormalizeName(string? value) =>
        string.Join(
            ' ',
            (value ?? string.Empty).Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

    private string FirstModelError() => ModelState.Values
        .SelectMany(item => item.Errors)
        .Select(item => item.ErrorMessage)
        .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))
        ?? "Please check the submitted fields.";

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
            RoleId = item.RoleId,
            Scope = item.Scope,
            Status = item.Status,
            InvitedAtUtc = item.InvitedAtUtc,
            AcceptedAtUtc = item.AcceptedAtUtc,
            IsFounder = item.IsFounder,
            CanChangeRole = item.CanChangeRole,
            CanRemove = item.CanRemove,
            AllowedRoles = item.AllowedRoles ?? []
        };
    }

    private static CompanyAccessRoleViewModel ToAccessRoleViewModel(
        CompanyAccessRoleApiItem item) => new()
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Scope = item.Scope,
            IsSystem = item.IsSystem,
            IsFullAccess = item.IsFullAccess,
            ParticipantCount = item.ParticipantCount,
            PermissionKeys = item.PermissionKeys ?? []
        };

    private static CompanyAccessHistoryViewModel ToAccessHistoryViewModel(
        CompanyAccessHistoryApiItem item) => new()
        {
            Id = item.Id,
            EventType = item.EventType,
            Summary = item.Summary,
            Details = item.Details,
            ActorUserId = item.ActorUserId,
            ActorName = item.ActorName,
            ActorEmail = item.ActorEmail,
            TargetUserId = item.TargetUserId,
            TargetName = item.TargetName,
            TargetEmail = item.TargetEmail,
            RoleId = item.RoleId,
            RoleName = item.RoleName,
            CreatedAtUtc = item.CreatedAtUtc
        };

    private static CompanyPermissionGroupViewModel ToPermissionGroupViewModel(
        CompanyPermissionGroupApiItem item) => new()
        {
            Key = item.Key,
            Label = item.Label,
            Permissions = (item.Permissions ?? []).Select(permission =>
                new CompanyPermissionViewModel
                {
                    Key = permission.Key,
                    Label = permission.Label,
                    Sensitive = permission.Sensitive
                }).ToList()
        };

    private static string NormalizeTeamTab(string? tab) =>
        tab?.Trim().ToLowerInvariant() switch
        {
            "roles" => "roles",
            "history" => "history",
            _ => "participants"
        };

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
