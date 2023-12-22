using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SmallRss.Web.Authorisation;

public interface IAuthorisationHandler
{
    Task<(bool IsReturningUser, string VerifyOptions)> HandleSigninRequest(string email);
    Task<bool> HandleSigninVerifyRequest(HttpContext httpContext, string email, string verifyOptions, string verifyResponse, CancellationToken cancellationToken);
}
