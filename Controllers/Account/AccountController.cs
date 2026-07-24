using System.Security.Claims;
using GloryLikeWebApp.Models.Auth;
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

    public AccountController(IBackendAuthApiService authApiService)
    {
        _authApiService = authApiService;
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
    public IActionResult Registration()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new RegistrationViewModel
            {
                AccountType = "employer",
                CompanyType = "SME"
            });
        }

        return RedirectToSelectedPortal();
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

        model.AccountType =
            model.AccountType?
                .Trim()
                .ToLowerInvariant()
            ?? string.Empty;
        model.ProfileName =
            model.ProfileName?.Trim() ?? string.Empty;
        model.Email =
            model.Email?.Trim() ?? string.Empty;
        model.CompanyType =
            model.CompanyType?.Trim();
        model.Industry =
            model.Industry?.Trim();

        if (model.AccountType == "employer")
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
        }
        else if (model.AccountType == "candidate")
        {
            model.CompanyType = null;
            model.Industry = null;
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

        await SignInUserAsync(
            result.User,
            portalType);

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

        await SignInUserAsync(
            result.User,
            portalType: null);

        // Login düzgündür. Portal hələ seçilməyib.
        return RedirectToAction(nameof(ChoosePortal));
    }

    [Authorize]
    [HttpGet("/ChoosePortal")]
    public IActionResult ChoosePortal()
    {
        return View(new PortalSelectionViewModel
        {
            PortalType =
                User.FindFirstValue(PortalClaimTypes.ClaimName)
                ?? string.Empty
        });
    }

    [Authorize]
    [HttpPost("/ChoosePortal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChoosePortal(
        PortalSelectionViewModel model)
    {
        var normalizedPortal =
            model.PortalType?
                .Trim()
                .ToLowerInvariant()
            ?? string.Empty;

        if (normalizedPortal is not
            (PortalClaimTypes.Employee or PortalClaimTypes.Employer))
        {
            ModelState.AddModelError(
                nameof(model.PortalType),
                "Employee və ya Employer seçilməlidir.");

            return View(model);
        }

        var authenticationResult =
            await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticationResult.Succeeded
            || authenticationResult.Principal is null)
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(SignIn));
        }

        var claims = authenticationResult.Principal.Claims
            .Where(
                claim => !string.Equals(
                    claim.Type,
                    PortalClaimTypes.ClaimName,
                    StringComparison.Ordinal))
            .ToList();

        claims.Add(
            new Claim(
                PortalClaimTypes.ClaimName,
                normalizedPortal));

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var properties =
            authenticationResult.Properties
            ?? CreateAuthenticationProperties();

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        return normalizedPortal == PortalClaimTypes.Employer
            ? RedirectToAction(
                "EmployerHome",
                "EmployerHome")
            : RedirectToAction(
                "Index",
                "Home");
    }

    [Authorize]
    [HttpGet("/SwitchPortal")]
    public IActionResult SwitchPortal()
    {
        return RedirectToAction(nameof(ChoosePortal));
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
        var portalType =
            User.FindFirstValue(
                PortalClaimTypes.ClaimName);

        return portalType switch
        {
            PortalClaimTypes.Employer =>
                RedirectToAction(
                    "EmployerHome",
                    "EmployerHome"),

            PortalClaimTypes.Employee =>
                RedirectToAction(
                    "Index",
                    "Home"),

            _ =>
                RedirectToAction(
                    nameof(ChoosePortal))
        };
    }

    private async Task SignInUserAsync(
        AuthUserDto user,
        string? portalType)
    {
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

        if (!string.IsNullOrWhiteSpace(portalType))
        {
            claims.Add(
                new Claim(
                    PortalClaimTypes.ClaimName,
                    portalType));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            CreateAuthenticationProperties());
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
