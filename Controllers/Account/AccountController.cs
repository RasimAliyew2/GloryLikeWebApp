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
    private readonly IConfiguration _configuration;

    public AccountController(
        IBackendAuthApiService authApiService,
        ICompanyTeamApiService companyTeamApiService,
        IConfiguration configuration)
    {
        _authApiService = authApiService;
        _companyTeamApiService = companyTeamApiService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("/SignIn")]
    public IActionResult SignIn(
        [FromQuery] string? externalError)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            PopulateExternalProviderViewData();

            if (!string.IsNullOrWhiteSpace(externalError))
            {
                ViewData["ExternalLoginError"] =
                    "Social sign in ləğv edildi və ya provayder cavab vermədi.";
            }

            return View(new LoginViewModel());
        }

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
    [HttpGet("/SignIn/External/{provider}")]
    public async Task<IActionResult> ExternalLogin(string provider)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToSelectedPortal();

        var scheme = ResolveExternalScheme(provider);

        if (scheme is null || !IsProviderConfigured(scheme))
        {
            TempData["ExternalLoginError"] =
                "Bu social sign in provayderi hələ konfiqurasiya edilməyib.";

            return RedirectToAction(nameof(SignIn));
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(
                nameof(ExternalLoginCallback),
                "Account")
        };
        properties.Items["LoginProvider"] = scheme;

        await ClearExternalCookieAsync();

        return Challenge(properties, scheme);
    }

    [AllowAnonymous]
    [HttpGet("/SignIn/External/Callback")]
    public async Task<IActionResult> ExternalLoginCallback(
        CancellationToken cancellationToken)
    {
        var externalResult = await HttpContext
            .AuthenticateAsync(
                ExternalAuthenticationDefaults
                    .ExternalCookieScheme);

        if (!externalResult.Succeeded
            || externalResult.Principal is null)
        {
            await ClearExternalCookieAsync();

            return ExternalLoginFailure(
                "Social hesab təsdiqlənmədi. Yenidən cəhd edin.");
        }

        var principal = externalResult.Principal;
        string? scheme = null;
        externalResult.Properties?.Items.TryGetValue(
            "LoginProvider",
            out scheme);
        var provider = scheme switch
        {
            ExternalAuthenticationDefaults.GoogleScheme =>
                "google",
            ExternalAuthenticationDefaults.AppleScheme =>
                "apple",
            _ => string.Empty
        };
        var providerSubject = FirstClaimValue(
            principal,
            ClaimTypes.NameIdentifier,
            "sub");
        var email = FirstClaimValue(
            principal,
            ClaimTypes.Email,
            "email");

        if (string.IsNullOrWhiteSpace(provider)
            || string.IsNullOrWhiteSpace(providerSubject)
            || string.IsNullOrWhiteSpace(email)
            || HasExplicitlyUnverifiedEmail(principal))
        {
            await ClearExternalCookieAsync();

            return ExternalLoginFailure(
                "Social hesabdan təsdiqlənmiş email alınmadı.");
        }

        var firstName = FirstClaimValue(
            principal,
            ClaimTypes.GivenName,
            "given_name");
        var lastName = FirstClaimValue(
            principal,
            ClaimTypes.Surname,
            "family_name");

        ApplyFullNameFallback(
            principal,
            ref firstName,
            ref lastName);

        var result = await _authApiService
            .SocialLoginAsync(
                new SocialLoginRequestDto
                {
                    Provider = provider,
                    ProviderSubject = providerSubject,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName
                },
                cancellationToken);

        await ClearExternalCookieAsync();

        if (!result.Success || result.User is null)
        {
            return ExternalLoginFailure(
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Social hesabla sign in tamamlanmadı."
                    : result.Message);
        }

        await SignInUserAsync(result.User);

        return ResolvePortalType(result.User.AccountType)
            == PortalClaimTypes.Employer
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
        {
            PopulateExternalProviderViewData();
            return View(model);
        }

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

            PopulateExternalProviderViewData();
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

    private string? ResolveExternalScheme(string? provider)
    {
        return provider?.Trim().ToLowerInvariant() switch
        {
            "google" =>
                ExternalAuthenticationDefaults.GoogleScheme,
            "apple" =>
                ExternalAuthenticationDefaults.AppleScheme,
            _ => null
        };
    }

    private bool IsProviderConfigured(string scheme)
    {
        if (scheme == ExternalAuthenticationDefaults.GoogleScheme)
        {
            return HasSetting(
                    "Authentication:Google:ClientId")
                && HasSetting(
                    "Authentication:Google:ClientSecret")
                && HasSetting(
                    "SocialAuth:BackendSharedSecret");
        }

        return scheme == ExternalAuthenticationDefaults.AppleScheme
            && HasSetting("Authentication:Apple:ClientId")
            && HasSetting("Authentication:Apple:TeamId")
            && HasSetting("Authentication:Apple:KeyId")
            && (HasSetting("Authentication:Apple:PrivateKey")
                || HasSetting(
                    "Authentication:Apple:PrivateKeyBase64"))
            && HasSetting("SocialAuth:BackendSharedSecret");
    }

    private void PopulateExternalProviderViewData()
    {
        ViewData["GoogleAuthenticationEnabled"] =
            IsProviderConfigured(
                ExternalAuthenticationDefaults.GoogleScheme);
        ViewData["AppleAuthenticationEnabled"] =
            IsProviderConfigured(
                ExternalAuthenticationDefaults.AppleScheme);
    }

    private bool HasSetting(string key)
    {
        return !string.IsNullOrWhiteSpace(
            _configuration[key]);
    }

    private IActionResult ExternalLoginFailure(string message)
    {
        TempData["ExternalLoginError"] = message;
        return RedirectToAction(nameof(SignIn));
    }

    private Task ClearExternalCookieAsync()
    {
        return HttpContext.SignOutAsync(
            ExternalAuthenticationDefaults
                .ExternalCookieScheme);
    }

    private static string FirstClaimValue(
        ClaimsPrincipal principal,
        params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);

            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static bool HasExplicitlyUnverifiedEmail(
        ClaimsPrincipal principal)
    {
        var value = FirstClaimValue(
            principal,
            "email_verified");

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (bool.TryParse(value, out var verified))
            return !verified;

        return value != "1";
    }

    private static void ApplyFullNameFallback(
        ClaimsPrincipal principal,
        ref string firstName,
        ref string lastName)
    {
        if (!string.IsNullOrWhiteSpace(firstName)
            && !string.IsNullOrWhiteSpace(lastName))
        {
            return;
        }

        var fullName = FirstClaimValue(
            principal,
            ClaimTypes.Name,
            "name");
        var parts = fullName.Split(
            ' ',
            2,
            StringSplitOptions.RemoveEmptyEntries
            | StringSplitOptions.TrimEntries);

        if (string.IsNullOrWhiteSpace(firstName)
            && parts.Length > 0)
        {
            firstName = parts[0];
        }

        if (string.IsNullOrWhiteSpace(lastName)
            && parts.Length > 1)
        {
            lastName = parts[1];
        }
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
