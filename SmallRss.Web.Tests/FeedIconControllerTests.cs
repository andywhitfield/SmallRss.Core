using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using SmallRss.Data;

namespace SmallRss.Web.Tests;

[TestClass]
public class FeedIconControllerTests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    public async Task Should_proxy_rss_feed_image(int rssFeedId)
    {
        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync($"/feedicon/{rssFeedId}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var icon = await response.Content.ReadAsByteArrayAsync();
        Assert.AreSequenceEqual(await File.ReadAllBytesAsync("favicon.ico"), icon);
    }

    [TestMethod]
    public async Task Should_return_missing_icon_for_feed_with_no_image()
    {
        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync($"/feedicon/2");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var icon = await response.Content.ReadAsByteArrayAsync();
        Assert.AreNotSequenceEqual(await File.ReadAllBytesAsync("favicon.ico"), icon);
    }

    [TestInitialize]
    public async Task SetupAsync()
    {
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(await File.ReadAllBytesAsync("favicon.ico")),
            });

        HttpClient client = new(mockHttpMessageHandler.Object);
        _webApplicationFactory.MockHttpClientFactory.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(client);

        
        await _webApplicationFactory.CreateTestUserAsync();
        await using var services = _webApplicationFactory.Services.CreateAsyncScope();
        var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.RssFeeds!.Add(new() { Id = 1, Uri = "http://test-feed-1.com", ImageUrl = "http://test-feed-1.com/image.png" });
        context.RssFeeds!.Add(new() { Id = 2, Uri = "http://test-feed-2.com"});
        context.RssFeeds!.Add(new() { Id = 3, Uri = "http://test-feed-3.com", ImageUrl = "http://test-feed-3.com/image.png" });
        context.UserFeeds!.Add(new() { GroupName = "test-group-1", Name = "test-feed-1", RssFeedId = 1, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { GroupName = "test-group-2", Name = "test-feed-2", RssFeedId = 2, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { GroupName = "test-group-2", Name = "test-feed-3", RssFeedId = 3, UserAccountId = _webApplicationFactory.TestUser.Id });
        await context.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}