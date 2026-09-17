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
        if (GloryLikeWebApp.Security.AccountRouting.IsSupported(accountType))
            return LocalRedirect(GloryLikeWebApp.Security.AccountRouting.HomePath(accountType));
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/SignIn");
    }
}
