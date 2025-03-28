using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SmallRss.Data;
using SmallRss.Models;

namespace SmallRss.Web.Authorisation;

public class AuthorisationHandler(ILogger<AuthorisationHandler> logger,
    IUserAccountRepository userAccountRepository, IFido2 fido2)
    : IAuthorisationHandler
{
    public async Task<(bool IsReturningUser, string VerifyOptions)> HandleSigninRequest(string email)
    {
        UserAccount? user;
        string options;
        if ((user = await userAccountRepository.FindByEmailAsync(email)) != null)
        {
            logger.LogTrace($"Found existing user account with email [{email}], creating assertion options");
            options = fido2.GetAssertionOptions(
                user.UserAccountCredentials
                    .Select(uac => new PublicKeyCredentialDescriptor(uac.CredentialId))
                    .ToArray(),
                UserVerificationRequirement.Discouraged
            ).ToJson();
        }
        else
        {
            logger.LogTrace($"Found no user account with email [{email}], creating request new creds options");
            options = fido2.RequestNewCredential(
                new Fido2User() { Id = Encoding.UTF8.GetBytes(email), Name = email, DisplayName = email },
                [],
                AuthenticatorSelection.Default,
                AttestationConveyancePreference.None
            ).ToJson();
        }

        logger.LogTrace($"Created sign in options: {options}");

        return (user != null, options);        
    }

    public async Task<bool> HandleSigninVerifyRequest(HttpContext httpContext, string email, string verifyOptions, string verifyResponse, CancellationToken cancellationToken)
    {
        UserAccount? user;
        if ((user = await userAccountRepository.FindByEmailAsync(email)) != null)
        {
            if (!await SigninUserAsync(user, verifyOptions, verifyResponse, cancellationToken))
                return false;
        }
        else
        {
            user = await CreateNewUserAsync(email, verifyOptions, verifyResponse, cancellationToken);
            if (user == null)
                return false;
        }

        logger.LogTrace($"Setting identity to [{user.Email}]");
        List<Claim> claims = [new Claim(ClaimTypes.Name, user.Email)];
        ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        AuthenticationProperties authProperties = new() { IsPersistent = true };
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        logger.LogTrace($"Signed in: {email}");

        return true;
    }

    private async Task<UserAccount?> CreateNewUserAsync(string email, string verifyOptions, string verifyResponse, CancellationToken cancellationToken)
    {
        logger.LogTrace("Creating new user credientials");
        var options = CredentialCreateOptions.FromJson(verifyOptions);

        AuthenticatorAttestationRawResponse? authenticatorAttestationRawResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(verifyResponse);
        if (authenticatorAttestationRawResponse == null)
        {
            logger.LogWarning($"Cannot parse signin verify response: {verifyResponse}");
            return null;
        }

        logger.LogTrace($"Successfully parsed response: {verifyResponse}");

        var success = await fido2.MakeNewCredentialAsync(authenticatorAttestationRawResponse, options, (_, _) => Task.FromResult(true), cancellationToken: cancellationToken);
        logger.LogInformation($"got success status: {success.Status} error: {success.ErrorMessage}");
        if (success.Result == null)
        {
            logger.LogWarning($"Could not create new credential: {success.Status} - {success.ErrorMessage}");
            return null;
        }

        logger.LogTrace($"Got new credential: {JsonSerializer.Serialize(success.Result)}");

        return await userAccountRepository.CreateAsync(email, success.Result.CredentialId,
            success.Result.PublicKey, success.Result.User.Id);
    }

    private async Task<bool> SigninUserAsync(UserAccount user, string verifyOptions, string verifyResponse, CancellationToken cancellationToken)
    {
        logger.LogTrace($"Checking credientials: {verifyResponse}");
        AuthenticatorAssertionRawResponse? authenticatorAssertionRawResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(verifyResponse);
        if (authenticatorAssertionRawResponse == null)
        {
            logger.LogWarning($"Cannot parse signin assertion verify response: {verifyResponse}");
            return false;
        }
        var options = AssertionOptions.FromJson(verifyOptions);
        var userAccountCredential = user.UserAccountCredentials.FirstOrDefault(uac => uac.CredentialId.SequenceEqual(authenticatorAssertionRawResponse.Id));
        if (userAccountCredential == null)
        {
            logger.LogWarning($"No credential id [{Convert.ToBase64String(authenticatorAssertionRawResponse.Id)}] for user [{user.Email}]");
            return false;
        }
        
        logger.LogTrace($"Making assertion for user [{user.Email}]");
        var res = await fido2.MakeAssertionAsync(authenticatorAssertionRawResponse, options, userAccountCredential.PublicKey, userAccountCredential.SignatureCount, VerifyExistingUserCredentialAsync, cancellationToken: cancellationToken);
        if (!string.IsNullOrEmpty(res.ErrorMessage))
        {
            logger.LogWarning($"Signin assertion failed: {res.Status} - {res.ErrorMessage}");
            return false;
        }

        logger.LogTrace($"Signin success, got response: {JsonSerializer.Serialize(res)}");
        userAccountCredential.SignatureCount = res.Counter;
        await userAccountRepository.UpdateAsync(user);

        return true;
    }

    private async Task<bool> VerifyExistingUserCredentialAsync(IsUserHandleOwnerOfCredentialIdParams credentialIdUserHandleParams, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Checking credential {credentialIdUserHandleParams.CredentialId} - {credentialIdUserHandleParams.UserHandle}");
        var userAccount = await userAccountRepository.FindByUserHandleAsync(credentialIdUserHandleParams.UserHandle);
        return userAccount?
            .UserAccountCredentials
            .FirstOrDefault(uac => uac.UserHandle.SequenceEqual(credentialIdUserHandleParams.UserHandle))
            ?.CredentialId.SequenceEqual(credentialIdUserHandleParams.CredentialId) ?? false;
    }
}
