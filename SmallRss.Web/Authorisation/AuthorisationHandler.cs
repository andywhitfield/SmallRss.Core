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
            logger.LogTrace("Found existing user account with email [{Email}], creating assertion options", email);
            options = fido2.GetAssertionOptions(new()
            {
                AllowedCredentials = [.. user.UserAccountCredentials.Select(uac => new PublicKeyCredentialDescriptor(uac.CredentialId))],
                UserVerification = UserVerificationRequirement.Discouraged
            }
            ).ToJson();
        }
        else
        {
            logger.LogTrace("Found no user account with email [{Email}], creating request new creds options", email);
            options = fido2.RequestNewCredential(new()
            {
                User = new Fido2User() { Id = Encoding.UTF8.GetBytes(email), Name = email, DisplayName = email },
                ExcludeCredentials = [],
                AuthenticatorSelection = AuthenticatorSelection.Default,
                AttestationPreference = AttestationConveyancePreference.None
            }
            ).ToJson();
        }

        logger.LogTrace("Created sign in options: {Options}", options);

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

        logger.LogTrace("Setting identity to [{UserEmail}]", user.Email);
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
            logger.LogWarning("Cannot parse signin verify response: {VerifyResponse}", verifyResponse);
            return null;
        }

        logger.LogTrace("Successfully parsed response: {VerifyResponse}", verifyResponse);

        var success = await fido2.MakeNewCredentialAsync(new()
        {
            AttestationResponse = authenticatorAttestationRawResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = (_, _) => Task.FromResult(true)
        }, cancellationToken: cancellationToken);
        logger.LogInformation("got success status: {Success}", success);
        if (success == null)
        {
            logger.LogWarning("Could not create new credential");
            return null;
        }

        logger.LogTrace("Got new credential: {Success}", JsonSerializer.Serialize(success));

        return await userAccountRepository.CreateAsync(email, success.Id,
            success.PublicKey, success.User.Id);
    }

    private async Task<bool> SigninUserAsync(UserAccount user, string verifyOptions, string verifyResponse, CancellationToken cancellationToken)
    {
        logger.LogTrace("Checking credientials: {VerifyResponse}", verifyResponse);
        AuthenticatorAssertionRawResponse? authenticatorAssertionRawResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(verifyResponse);
        if (authenticatorAssertionRawResponse == null)
        {
            logger.LogWarning("Cannot parse signin assertion verify response: {VerifyResponse}", verifyResponse);
            return false;
        }
        var options = AssertionOptions.FromJson(verifyOptions);
        var userAccountCredential = user.UserAccountCredentials.FirstOrDefault(uac => uac.CredentialId.SequenceEqual(authenticatorAssertionRawResponse.RawId));
        if (userAccountCredential == null)
        {
            logger.LogWarning("No credential id [{AuthenticatorAssertionRawResponseRawId}] for user [{UserEmail}]", Convert.ToBase64String(authenticatorAssertionRawResponse.RawId), user.Email);
            return false;
        }

        logger.LogTrace("Making assertion for user [{UserEmail}]", user.Email);
        var res = await fido2.MakeAssertionAsync(new()
        {
            AssertionResponse = authenticatorAssertionRawResponse,
            OriginalOptions = options,
            StoredPublicKey = userAccountCredential.PublicKey,
            StoredSignatureCounter = userAccountCredential.SignatureCount,
            IsUserHandleOwnerOfCredentialIdCallback = VerifyExistingUserCredentialAsync
        }, cancellationToken: cancellationToken);

        logger.LogTrace("Signin success, got response: {Res}", JsonSerializer.Serialize(res));
        userAccountCredential.SignatureCount = res.SignCount;
        await userAccountRepository.UpdateAsync(user);

        return true;
    }

    private async Task<bool> VerifyExistingUserCredentialAsync(IsUserHandleOwnerOfCredentialIdParams credentialIdUserHandleParams, CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking credential {CredentialId} - {UserHandle}", credentialIdUserHandleParams.CredentialId, credentialIdUserHandleParams.UserHandle);
        var userAccount = await userAccountRepository.FindByUserHandleAsync(credentialIdUserHandleParams.UserHandle);
        return userAccount?
            .UserAccountCredentials
            .FirstOrDefault(uac => uac.UserHandle.SequenceEqual(credentialIdUserHandleParams.UserHandle))
            ?.CredentialId.SequenceEqual(credentialIdUserHandleParams.CredentialId) ?? false;
    }
}
