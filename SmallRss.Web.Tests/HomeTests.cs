using System.Net;

namespace SmallRss.Web.Tests;

[TestClass]
public class HomeTests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    [TestMethod]
    public async Task Given_no_credentials_should_redirect_to_login()
    {
        using var client = await _webApplicationFactory.CreateUnauthenticatedClientAsync();
        using var response = await client.GetAsync("/");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Given_valid_credentials_should_be_logged_in()
    {
        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync("/");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(responseContent, "Logout");
    }
}