using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmallRss.Data;

namespace SmallRss.Web.Tests;

[TestClass]
public class Article_MarkAsRead_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(2, 1)]
    [DataRow(3, 2)]
    [DataRow(4, 2)]
    [DataRow(5, 2)]
    public async Task Mark_single_story_read(int storyId, int expectedUserFeedId)
    {
        await CreateTestArticlesAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.PostAsync("/api/article/", new FormUrlEncodedContent(new Dictionary<string, string> { { "storyId", storyId.ToString() }, { "read", "true" } }));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(responseContent);
        Assert.AreEqual(0, responseContent.Length);

        await AssertDbAsync();
        async Task AssertDbAsync()
        {
            await using var services = _webApplicationFactory.Services.CreateAsyncScope();
            var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
            Assert.AreEqual(1, await context.UserArticlesRead!.CountAsync());
            var articleRead = await context.UserArticlesRead!.SingleAsync();
            Assert.AreEqual(storyId, articleRead.ArticleId);
            Assert.AreEqual(_webApplicationFactory.TestUser.Id, articleRead.UserAccountId);
            Assert.AreEqual(expectedUserFeedId, articleRead.UserFeedId);
        }
    }

    [TestMethod]
    public async Task Cannot_mark_story_read_when_not_in_user_feed()
    {
        await CreateTestArticlesAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.PostAsync("/api/article/", new FormUrlEncodedContent(new Dictionary<string, string> { { "storyId", "6" }, { "read", "true" } }));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(responseContent);
        Assert.AreEqual(0, responseContent.Length);

        await AssertDbAsync();
        async Task AssertDbAsync()
        {
            await using var services = _webApplicationFactory.Services.CreateAsyncScope();
            var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
            Assert.AreEqual(0, await context.UserArticlesRead!.CountAsync());
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
        context.UserFeeds!.Add(new() { GroupName = "test-group-1", Name = "test-feed-1", RssFeedId = 1, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { GroupName = "test-group-1", Name = "test-feed-2", RssFeedId = 2, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { GroupName = "test-group-1-acc-10", Name = "test-feed-3", RssFeedId = 3, UserAccountId = userAccount10.Entity.Id });
        context.Articles!.Add(new() { Id = 1, RssFeedId = 1, ArticleGuid = "article-1", Heading = "article 1" });
        context.Articles!.Add(new() { Id = 2, RssFeedId = 1, ArticleGuid = "article-2", Heading = "article 2" });
        context.Articles!.Add(new() { Id = 3, RssFeedId = 2, ArticleGuid = "article-3", Heading = "article 3" });
        context.Articles!.Add(new() { Id = 4, RssFeedId = 2, ArticleGuid = "article-4", Heading = "article 4" });
        context.Articles!.Add(new() { Id = 5, RssFeedId = 2, ArticleGuid = "article-5", Heading = "article 5" });
        context.Articles!.Add(new() { Id = 6, RssFeedId = 3, ArticleGuid = "article-6", Heading = "article 6" });
        await context.SaveChangesAsync();
    }
}