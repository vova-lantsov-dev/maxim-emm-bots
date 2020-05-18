using System;
using System.Threading;
using System.Threading.Tasks;
using Host.Authentication;
using Host.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Host.Controllers
{
    [Route("auth")]
    [AllowAnonymous]
    public sealed class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult> LogIn([FromForm] LogInModel model, CancellationToken ct)
        {
            ApplicationUser user = await _authService.AuthenticateUserAsync(model.Username, model.Password, ct);
            if (user == null)
                return RedirectToAction("Auth");

            string token = _authService.GenerateJwtToken(user);
            Response.Cookies.Append("auth", token, new CookieOptions
            {
                MaxAge = TimeSpan.FromMinutes(30d),
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });
            
            return Redirect("/hangfire");
        }

        [HttpGet]
        public ActionResult Auth()
        {
            return View();
        }
    }
}