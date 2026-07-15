using GloryLikeWebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers.Account
{
    public class AccountController : Controller
    {

        [HttpGet("/SignIn")]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost("/SignIn")]
        public IActionResult SignIn(UserInfo user)
        {
            return View();
        }
    }
}
