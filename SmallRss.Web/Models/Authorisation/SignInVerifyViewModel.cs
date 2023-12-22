namespace SmallRss.Web.Models.Authorisation;

public class SignInVerifyViewModel(string? returnUrl, string email, bool isReturningUser, string verifyOptions)
{
    public string? ReturnUrl { get; } = returnUrl;
    public string Email { get; } = email;
    public bool IsReturningUser { get; } = isReturningUser;
    public string VerifyOptions { get; } = verifyOptions;
}
