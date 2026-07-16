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

        var user = result.User;

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
                user.UserName ?? string.Empty)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            CreateAuthenticationProperties());

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
