using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SmallRss.Web.Authorisation;
using SmallRss.Web.Models.Authorisation;

namespace SmallRss.Web.Controllers;

public class AuthenticationController(IAuthorisationHandler authorisationHandler) : Controller
{
    [HttpGet("~/signin")]
    public IActionResult Signin([FromQuery] string? returnUrl) => View("SignIn", returnUrl);

    [HttpPost("~/signin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signin([FromForm] string? returnUrl, [FromForm, Required] string email)
    {
        if (!ModelState.IsValid)
            return View("SignIn", returnUrl);

        var (isReturningUser, verifyOptions) = await authorisationHandler.HandleSigninRequest(email);
        return View("SignInVerify", new SignInVerifyViewModel(returnUrl, email, isReturningUser, verifyOptions));
    }

    [HttpPost("~/signin/verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SigninVerify(
        [FromForm] string? returnUrl,
        [FromForm, Required] string email,
        [FromForm, Required] string verifyOptions,
        [FromForm, Required] string verifyResponse,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Redirect("~/signin");

        var isValid = await authorisationHandler.HandleSigninVerifyRequest(HttpContext, email, verifyOptions, verifyResponse, cancellationToken);
        if (isValid)
        {
            var redirectUri = "~/";
            if (!string.IsNullOrEmpty(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Relative, out var uri))
                redirectUri = uri.ToString();

            return Redirect(redirectUri);
        }
        
        return Redirect("~/signin");
    }

    [HttpGet("~/signout"), HttpPost("~/signout")]
    public async Task<IActionResult> Signout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("~/");
    }
}