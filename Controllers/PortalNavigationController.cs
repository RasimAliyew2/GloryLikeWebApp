using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize]
public sealed class PortalNavigationController : Controller
{
    [HttpGet("/Portal/Home")]
    public async Task<IActionResult> Home()
    {
        var accountType = User.FindFirstValue("accountType");

        if (string.Equals(
                accountType,
                "employer",
                StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("EmployerHome", "EmployerHome");
        }

        if (string.Equals(
                accountType,
                "candidate",
                StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Home");
        }

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/SignIn");
    }
}
