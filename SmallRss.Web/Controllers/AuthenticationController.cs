using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;

namespace SmallRss.Web.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly IUserAccountRepository userAccountRepository;
        private readonly ILogger<AuthenticationController> logger;

        public AuthenticationController(IUserAccountRepository userAccountRepository,
            ILogger<AuthenticationController> logger)
        {
            this.userAccountRepository = userAccountRepository;
            this.logger = logger;
        }

        [HttpGet("~/signin")]
        public IActionResult SignIn() => View("SignIn");

        [HttpPost("~/signin")]
        public IActionResult SignInChallenge()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet("~/signout"), HttpPost("~/signout")]
        public IActionResult SignOut()
        {
            return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}