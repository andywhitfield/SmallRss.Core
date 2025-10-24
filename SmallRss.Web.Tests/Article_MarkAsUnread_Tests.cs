using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmallRss.Data;

namespace SmallRss.Web.Tests;

[TestClass]
public class Article_MarkAsUnread_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(2, 0)]
    [DataRow(4, 1)]
    [DataRow(5, 1)]
    public async Task Mark_single_story_unread(int storyId, int expectedUserArticlesRead)
    {
        await CreateTestArticlesAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.PostAsync("/api/article/", new FormUrlEncodedContent(new Dictionary<string, string> { { "storyId", storyId.ToString() }, { "read", "false" } }));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(responseContent);
        Assert.IsEmpty(responseContent);

        await AssertDbAsync();
        async Task AssertDbAsync()
        {
            await using var services = _webApplicationFactory.Services.CreateAsyncScope();
            var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
            Assert.AreEqual(expectedUserArticlesRead, await context.UserArticlesRead!.CountAsync(uar => uar.ArticleId == storyId));
        }
    }

    [TestMethod]
    public async Task Cannot_mark_story_unread_when_not_in_user_feed()
    {
        await CreateTestArticlesAsync();
        await AssertDbAsync(1);

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.PostAsync("/api/article/", new FormUrlEncodedContent(new Dictionary<string, string> { { "storyId", "6" }, { "read", "false" } }));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(responseContent);
        Assert.IsEmpty(responseContent);

        await AssertDbAsync(1);
        async Task AssertDbAsync(int expectedCount)
        {
            await using var services = _webApplicationFactory.Services.CreateAsyncScope();
            var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
            Assert.AreEqual(expectedCount, await context.UserArticlesRead!.CountAsync(uar => uar.ArticleId == 6));
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    // marking 'all unread' is not a feature...we only go one way at the moment - marking all read
    public async Task Mark_all_unread_does_nothing_for_one_userfeed(int userFeedId)
    {
        await CreateTestArticlesAsync();
        await AssertDbAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.PostAsync("/api/article/", new FormUrlEncodedContent(new Dictionary<string, string> { { "feedId", userFeedId.ToString() }, { "read", "false" } }));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(responseContent);
        Assert.IsEmpty(responseContent);

        await AssertDbAsync();
        async Task AssertDbAsync()
        {
            await using var services = _webApplicationFactory.Services.CreateAsyncScope();
            var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
            Assert.AreEqual(2, await context.UserArticlesRead!.CountAsync(uar => uar.UserFeedId == userFeedId));
        }
    }

    [TestMethod]
    public async Task Mark_all_unread_does_nothing_for_all_feeds()
    {
        await CreateTestArticlesAsync();
        await AssertDbAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.PostAsync("/api/article/", new FormUrlEncodedContent(new Dictionary<string, string> { { "feedId", "-1" }, { "read", "false" } }));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(responseContent);
        Assert.IsEmpty(responseContent);

        await AssertDbAsync();
        async Task AssertDbAsync()
        {
            await using var services = _webApplicationFactory.Services.CreateAsyncScope();
            var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
            Assert.AreEqual(8, await context.UserArticlesRead!.CountAsync());
        }
    }

    private async Task CreateTestArticlesAsync()
    {
        await _webApplicationFactory.CreateTestUserAsync();
        await using var services = _webApplicationFactory.Services.CreateAsyncScope();
        var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var userAccount10 = context.UserAccounts!.Add(new() { Id = 10, Email = "test-account-10@test.com" });
        context.RssFeeds!.Add(new() { Id = 1, Uri = "http://test-feed-1.com" });
        context.RssFeeds!.Add(new() { Id = 2, Uri = "http://test-feed-2.com" });
        context.RssFeeds!.Add(new() { Id = 3, Uri = "http://test-feed-3.com" });
        context.UserFeeds!.Add(new() { Id = 1, GroupName = "test-group-1", Name = "test-feed-1", RssFeedId = 1, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { Id = 2, GroupName = "test-group-1", Name = "test-feed-2", RssFeedId = 2, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { Id = 3, GroupName = "test-group-1-acc-10", Name = "test-feed-3", RssFeedId = 2, UserAccountId = userAccount10.Entity.Id });
        context.UserFeeds!.Add(new() { Id = 4, GroupName = "test-group-2-acc-10", Name = "test-feed-4", RssFeedId = 3, UserAccountId = userAccount10.Entity.Id });
        context.Articles!.Add(new() { Id = 1, RssFeedId = 1, ArticleGuid = "article-1", Heading = "article 1" });
        context.Articles!.Add(new() { Id = 2, RssFeedId = 1, ArticleGuid = "article-2", Heading = "article 2" });
        context.Articles!.Add(new() { Id = 3, RssFeedId = 2, ArticleGuid = "article-3", Heading = "article 3" });
        context.Articles!.Add(new() { Id = 4, RssFeedId = 2, ArticleGuid = "article-4", Heading = "article 4" });
        context.Articles!.Add(new() { Id = 5, RssFeedId = 2, ArticleGuid = "article-5", Heading = "article 5" });
        context.Articles!.Add(new() { Id = 6, RssFeedId = 3, ArticleGuid = "article-6", Heading = "article 6" });
        context.UserArticlesRead!.Add(new() { UserAccountId = _webApplicationFactory.TestUser.Id, UserFeedId = 1, ArticleId = 1 });
        context.UserArticlesRead!.Add(new() { UserAccountId = _webApplicationFactory.TestUser.Id, UserFeedId = 1, ArticleId = 2 });
        context.UserArticlesRead!.Add(new() { UserAccountId = _webApplicationFactory.TestUser.Id, UserFeedId = 2, ArticleId = 4 });
        context.UserArticlesRead!.Add(new() { UserAccountId = _webApplicationFactory.TestUser.Id, UserFeedId = 2, ArticleId = 5 });
        context.UserArticlesRead!.Add(new() { UserAccountId = userAccount10.Entity.Id, UserFeedId = 3, ArticleId = 3 });
        context.UserArticlesRead!.Add(new() { UserAccountId = userAccount10.Entity.Id, UserFeedId = 3, ArticleId = 4 });
        context.UserArticlesRead!.Add(new() { UserAccountId = userAccount10.Entity.Id, UserFeedId = 3, ArticleId = 5 });
        context.UserArticlesRead!.Add(new() { UserAccountId = userAccount10.Entity.Id, UserFeedId = 4, ArticleId = 6 });
        await context.SaveChangesAsync();
    }
}