using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SmallRss.Data;

namespace SmallRss.Web.Tests;

[TestClass]
public class Feed_Get_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    [TestMethod]
    public async Task Get_feeds()
    {
        await CreateTestUserFeedsAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync("/api/feed");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<List<FeedGroup>>();
        Assert.AreEqual(3, responseContent?.Count);

        var feedGroup = responseContent![0];
        Assert.AreEqual("test-group-1", feedGroup.id);
        Assert.AreEqual("test-group-1", feedGroup.item);
        Assert.IsTrue(feedGroup.props.isFolder);
        Assert.HasCount(1, feedGroup.items);

        Assert.AreEqual(1, feedGroup.items[0].id);
        Assert.AreEqual("test-feed-1", feedGroup.items[0].item);

        feedGroup = responseContent[1];
        Assert.AreEqual("test-group-2", feedGroup.id);
        Assert.AreEqual("test-group-2", feedGroup.item);
        Assert.IsTrue(feedGroup.props.isFolder);
        Assert.HasCount(2, feedGroup.items);

        Assert.AreEqual(2, feedGroup.items[0].id);
        Assert.AreEqual("test-feed-2", feedGroup.items[0].item);
        Assert.AreEqual(3, feedGroup.items[1].id);
        Assert.AreEqual("test-feed-3", feedGroup.items[1].item);

        feedGroup = responseContent[2];
        Assert.AreEqual("All unread", feedGroup.id);
        Assert.AreEqual("All unread", feedGroup.item);
        Assert.IsTrue(feedGroup.props.isFolder);
        Assert.HasCount(1, feedGroup.items);

        Assert.AreEqual(-1, feedGroup.items[0].id);
        Assert.AreEqual("All unread", feedGroup.items[0].item);
    }

    private async Task CreateTestUserFeedsAsync()
    {
        await _webApplicationFactory.CreateTestUserAsync();
        await using var services = _webApplicationFactory.Services.CreateAsyncScope();
        var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.RssFeeds!.Add(new() { Id = 1, Uri = "http://test-feed-1.com" });
        context.RssFeeds!.Add(new() { Id = 2, Uri = "http://test-feed-2.com" });
        context.RssFeeds!.Add(new() { Id = 3, Uri = "http://test-feed-3.com" });
        context.UserFeeds!.Add(new() { GroupName = "test-group-1", Name = "test-feed-1", RssFeedId = 1, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { GroupName = "test-group-2", Name = "test-feed-2", RssFeedId = 2, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { GroupName = "test-group-2", Name = "test-feed-3", RssFeedId = 3, UserAccountId = _webApplicationFactory.TestUser.Id });
        await context.SaveChangesAsync();
    }

    private record FeedProps(bool isFolder);
    private record FeedItem(int id, string item, FeedProps props);
    private record FeedGroup(string id, string item, FeedProps props, List<FeedItem> items);
}