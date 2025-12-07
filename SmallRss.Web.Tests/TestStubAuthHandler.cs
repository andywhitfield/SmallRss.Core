using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmallRss.Web.Tests;

public class TestStubAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TestUserEmail = "test-user-1";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(Request.Headers["TESTAUTH"].Count != 0
            ? AuthenticateResult.Success(
                new AuthenticationTicket(
                    new(new ClaimsIdentity([new(ClaimTypes.GivenName, "Test user"), new(ClaimTypes.Name, TestUserEmail)], "Test")),
                    "Test"
                )
            )
            : AuthenticateResult.Fail("No auth provided"));
}