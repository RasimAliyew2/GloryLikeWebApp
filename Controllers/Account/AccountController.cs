using System.Security.Claims;
using GloryLikeWebApp.Models.Auth;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers.Account;

public sealed class AccountController : Controller
{
    private readonly IBackendAuthApiService _authApiService;
    private readonly ICompanyTeamApiService _companyTeamApiService;

    public AccountController(
        IBackendAuthApiService authApiService,
        ICompanyTeamApiService companyTeamApiService)
    {
        _authApiService = authApiService;
        _companyTeamApiService = companyTeamApiService;
    }

    [AllowAnonymous]
    [HttpGet("/SignIn")]
    public IActionResult SignIn()
    {
        if (User.Identity?.IsAuthenticated != true)
            return View(new LoginViewModel());

        return RedirectToSelectedPortal();
    }

    [AllowAnonymous]
    [HttpGet("/Registration")]
    public async Task<IActionResult> Registration(
        [FromQuery] string? invite,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToSelectedPortal();

        var model = new RegistrationViewModel
        {
            AccountType = "employer",
            CompanyType = "SME"
        };

        if (string.IsNullOrWhiteSpace(invite))
            return View(model);

        model.InvitationToken = invite.Trim();

        var invitation =
            await _companyTeamApiService.ResolveInvitationAsync(
                model.InvitationToken,
                cancellationToken);

        if (!invitation.Success
            || invitation.Data is null)
        {
            model.InvitationErrorMessage =
                string.IsNullOrWhiteSpace(invitation.Message)
                    ? "Invitation link düzgün deyil və ya vaxtı bitib."
                    : invitation.Message;

            return View(model);
        }

        ApplyInvitationToRegistrationModel(
            model,
            invitation.Data);

        return View(model);
    }

    [AllowAnonymous]
    [HttpPost("/Registration")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registration(
        RegistrationViewModel model,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToSelectedPortal();

        model.InvitationErrorMessage =
            string.Empty;
        model.InvitationToken =
            model.InvitationToken?.Trim();
        var isTeamInvitation =
            model.IsTeamInvitation;

        if (isTeamInvitation)
        {
            var invitation =
                await _companyTeamApiService
                    .ResolveInvitationAsync(
                        model.InvitationToken!,
                        cancellationToken);

            if (!invitation.Success
                || invitation.Data is null)
            {
                model.InvitationErrorMessage =
                    string.IsNullOrWhiteSpace(
                        invitation.Message)
                        ? "Invitation link düzgün deyil və ya vaxtı bitib."
                        : invitation.Message;

                return View(model);
            }

            ApplyInvitationToRegistrationModel(
                model,
                invitation.Data);

            RemoveInvitationManagedModelState();
        }

        model.AccountType =
            model.AccountType?
                .Trim()
                .ToLowerInvariant()
            ?? string.Empty;
        model.ProfileName =
            model.ProfileName?.Trim() ?? string.Empty;
        model.Email =
            model.Email?.Trim() ?? string.Empty;
        model.CompanyName =
            model.CompanyName?.Trim();
        model.CompanyType =
            model.CompanyType?.Trim();
        model.Industry =
            model.Industry?.Trim();
        model.InvitationRole =
            model.InvitationRole?.Trim();

        if (model.AccountType == "employer"
            && !isTeamInvitation)
        {
            if (model.CompanyType is not
                ("Startup" or "SME" or "Corporate"))
            {
                ModelState.AddModelError(
                    nameof(model.CompanyType),
                    "Company type seçilməlidir.");
            }

            if (string.IsNullOrWhiteSpace(model.Industry))
            {
                ModelState.AddModelError(
                    nameof(model.Industry),
                    "Industry daxil edin.");
            }

            model.CompanyName =
                model.ProfileName;
        }
        else if (model.AccountType == "candidate")
        {
            model.CompanyName = null;
            model.CompanyType = null;
            model.Industry = null;
            ModelState.Remove(nameof(model.CompanyName));
            ModelState.Remove(nameof(model.CompanyType));
            ModelState.Remove(nameof(model.Industry));
        }

        if (!ModelState.IsValid)
            return View(model);

        var result =
            await _authApiService.StartEmailRegistrationAsync(
                model,
                cancellationToken);

        if (!result.Success
            || result.VerificationId is null)
        {
            ModelState.AddModelError(
                string.Empty,
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Təsdiq kodu göndərilmədi. Yenidən cəhd edin."
                    : result.Message);

            return View(model);
        }

        TempData["RegistrationSuccessMessage"] =
            result.Message;

        return RedirectToAction(
            nameof(VerifyRegistration),
            new
            {
                verificationId =
                    result.VerificationId.Value
            });
    }

    private static void ApplyInvitationToRegistrationModel(
        RegistrationViewModel model,
        ResolveCompanyTeamInvitationApiResponse invitation)
    {
        model.AccountType = "employer";
        model.Email = invitation.Email;
        model.CompanyName = invitation.CompanyName;
        model.CompanyType = invitation.CompanyType;
        model.Industry = invitation.Industry;
        model.InvitationRole = invitation.Role;
    }

    private void RemoveInvitationManagedModelState()
    {
        ModelState.Remove(
            nameof(RegistrationViewModel.AccountType));
        ModelState.Remove(
            nameof(RegistrationViewModel.Email));
        ModelState.Remove(
            nameof(RegistrationViewModel.CompanyName));
        ModelState.Remove(
            nameof(RegistrationViewModel.CompanyType));
        ModelState.Remove(
            nameof(RegistrationViewModel.Industry));
        ModelState.Remove(
            nameof(RegistrationViewModel.InvitationRole));
        ModelState.Remove(
            nameof(RegistrationViewModel.InvitationToken));
    }

    [AllowAnonymous]
    [HttpGet("/Registration/Verify/{verificationId:guid}")]
    public async Task<IActionResult> VerifyRegistration(
        Guid verificationId,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToSelectedPortal();

        var result =
            await _authApiService.GetEmailRegistrationStatusAsync(
                verificationId,
                cancellationToken);

        var model = BuildVerifyRegistrationViewModel(
            verificationId,
            result);

        model.SuccessMessage =
            TempData["RegistrationSuccessMessage"]
                as string
            ?? string.Empty;

        model.ErrorMessage =
            TempData["RegistrationErrorMessage"]
                as string
            ?? model.ErrorMessage;

        return View(model);
    }

    [AllowAnonymous]
    [HttpPost("/Registration/Verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmRegistrationCode(
        VerifyRegistrationViewModel model,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToSelectedPortal();

        if (!ModelState.IsValid)
        {
            var status =
                await _authApiService
                    .GetEmailRegistrationStatusAsync(
                        model.VerificationId,
                        cancellationToken);

            var invalidModel =
                BuildVerifyRegistrationViewModel(
                    model.VerificationId,
                    status);
            invalidModel.Code = model.Code;

            return View(
                nameof(VerifyRegistration),
                invalidModel);
        }

        var result =
            await _authApiService.VerifyEmailRegistrationAsync(
                model.VerificationId,
                model.Code,
                cancellationToken);

        if (!result.Success || result.User is null)
        {
            var failedModel =
                BuildVerifyRegistrationViewModel(
                    model.VerificationId,
                    result);
            failedModel.Code = string.Empty;

            return View(
                nameof(VerifyRegistration),
                failedModel);
        }

        var portalType =
            string.Equals(
                result.User.AccountType,
                "employer",
                StringComparison.OrdinalIgnoreCase)
                ? PortalClaimTypes.Employer
                : PortalClaimTypes.Employee;

        await SignInUserAsync(result.User);

        return portalType == PortalClaimTypes.Employer
            ? RedirectToAction(
                "EmployerHome",
                "EmployerHome")
            : RedirectToAction(
                "Index",
                "Home");
    }

    [AllowAnonymous]
    [HttpPost("/Registration/Verify/Resend")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendRegistrationCode(
        Guid verificationId,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToSelectedPortal();

        var result =
            await _authApiService
                .ResendEmailRegistrationCodeAsync(
                    verificationId,
                    cancellationToken);

        if (result.Success)
        {
            TempData["RegistrationSuccessMessage"] =
                result.Message;
        }
        else
        {
            TempData["RegistrationErrorMessage"] =
                result.Message;
        }

        return RedirectToAction(
            nameof(VerifyRegistration),
            new { verificationId });
    }

    [AllowAnonymous]
    [HttpPost("/SignIn")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignIn(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.LoginAsync(
            model.Login,
            model.Password,
            cancellationToken);

        if (!result.Success || result.User is null)
        {
            ModelState.AddModelError(
                string.Empty,
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Password və ya login səhvdir."
                    : result.Message);

            return View(model);
        }

        var portalType = ResolvePortalType(
            result.User.AccountType);

        await SignInUserAsync(result.User);

        return portalType == PortalClaimTypes.Employer
            ? RedirectToAction(
                "EmployerHome",
                "EmployerHome")
            : RedirectToAction(
                "Index",
                "Home");
    }

    [Authorize]
    [HttpGet("/ChoosePortal")]
    public IActionResult ChoosePortal()
    {
        return RedirectToSelectedPortal();
    }

    [Authorize]
    [HttpPost("/ChoosePortal")]
    [ValidateAntiForgeryToken]
    public IActionResult ChoosePortal(
        PortalSelectionViewModel model)
    {
        return RedirectToSelectedPortal();
    }

    [Authorize]
    [HttpGet("/SwitchPortal")]
    public IActionResult SwitchPortal()
    {
        return RedirectToSelectedPortal();
    }

    [HttpPost("/SignOut")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignOutUser()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(SignIn));
    }

    private IActionResult RedirectToSelectedPortal()
    {
        var accountType = User.FindFirstValue("accountType");

        return string.Equals(
            accountType,
            "employer",
            StringComparison.OrdinalIgnoreCase)
                ? RedirectToAction(
                    "EmployerHome",
                    "EmployerHome")
                : RedirectToAction(
                "Index",
                "Home");
    }

    private async Task SignInUserAsync(
        AuthUserDto user)
    {
        var portalType = ResolvePortalType(user.AccountType);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Name,
                string.IsNullOrWhiteSpace(user.Name)
                    ? user.UserName
                    : user.Name),

            new(
                ClaimTypes.Surname,
                user.Surname ?? string.Empty),

            new(
                ClaimTypes.Email,
                user.Email ?? string.Empty),

            new(
                ClaimTypes.MobilePhone,
                user.PhoneNumber ?? string.Empty),

            new(
                "username",
                user.UserName ?? string.Empty),

            new(
                "accountType",
                user.AccountType ?? string.Empty)
        };

        claims.Add(
            new Claim(
                PortalClaimTypes.ClaimName,
                portalType));

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            CreateAuthenticationProperties());
    }

    private static string ResolvePortalType(string? accountType)
    {
        return string.Equals(
            accountType,
            "employer",
            StringComparison.OrdinalIgnoreCase)
                ? PortalClaimTypes.Employer
                : PortalClaimTypes.Employee;
    }

    private static VerifyRegistrationViewModel
        BuildVerifyRegistrationViewModel(
            Guid verificationId,
            EmailRegistrationResponseDto response)
    {
        return new VerifyRegistrationViewModel
        {
            VerificationId =
                response.VerificationId
                ?? verificationId,
            MaskedEmail = response.MaskedEmail,
            ExpiresAtUtc = response.ExpiresAtUtc,
            ResendAvailableAtUtc =
                response.ResendAvailableAtUtc,
            ExpiresInSeconds =
                response.ExpiresInSeconds,
            ResendInSeconds =
                response.ResendInSeconds,
            Expired =
                response.ExpiresAtUtc is null
                || response.Expired,
            CanResend = response.CanResend,
            ErrorMessage =
                response.Success
                    ? string.Empty
                    : response.Message
        };
    }

    private static AuthenticationProperties
        CreateAuthenticationProperties()
    {
        return new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc =
                DateTimeOffset.UtcNow.AddDays(7)
        };
    }
}
