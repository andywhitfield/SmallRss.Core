using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SmallRss.Data;

namespace SmallRss.Web.Tests;

[TestClass]
public class Feed_GetById_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    [TestMethod]
    public async Task Get_feed_articles()
    {
        await CreateTestUserFeedsAndArticlesAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync("/api/feed/1");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<List<FeedArticle>>();
        Assert.AreEqual(2, responseContent?.Count);

        var article = responseContent![0];
        Assert.IsFalse(article.read);
        Assert.AreEqual(1, article.feed);
        Assert.AreEqual(3, article.story);
        Assert.AreEqual("test-article-3-heading", article.heading);
        Assert.AreEqual("test-article-3-body", article.article);
        Assert.AreEqual($"{DateTime.Today.AddHours(-1):ddd} 21:00", article.posted);

        article = responseContent![1];
        Assert.IsFalse(article.read);
        Assert.AreEqual(1, article.feed);
        Assert.AreEqual(2, article.story);
        Assert.AreEqual("test-article-2-heading", article.heading);
        Assert.AreEqual("test-article-2-body", article.article);
        Assert.AreEqual($"{DateTime.Today.AddHours(-1):ddd} 22:00", article.posted);
    }

    [TestMethod]
    public async Task Get_all_unread_feed_articles()
    {
        await CreateTestUserFeedsAndArticlesAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync("/api/feed/-1");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<List<FeedArticle>>();
        Assert.AreEqual(3, responseContent?.Count);

        var article = responseContent![0];
        Assert.IsFalse(article.read);
        Assert.AreEqual(2, article.feed);
        Assert.AreEqual(4, article.story);
        Assert.AreEqual("test-article-4-heading", article.heading);
        Assert.AreEqual("test-article-4-body", article.article);
        Assert.AreEqual($"{DateTime.Today.AddHours(-1):ddd} 20:00", article.posted);

        article = responseContent![1];
        Assert.IsFalse(article.read);
        Assert.AreEqual(1, article.feed);
        Assert.AreEqual(3, article.story);
        Assert.AreEqual("test-article-3-heading", article.heading);
        Assert.AreEqual("test-article-3-body", article.article);
        Assert.AreEqual($"{DateTime.Today.AddHours(-1):ddd} 21:00", article.posted);

        article = responseContent![2];
        Assert.IsFalse(article.read);
        Assert.AreEqual(1, article.feed);
        Assert.AreEqual(2, article.story);
        Assert.AreEqual("test-article-2-heading", article.heading);
        Assert.AreEqual("test-article-2-body", article.article);
        Assert.AreEqual($"{DateTime.Today.AddHours(-1):ddd} 22:00", article.posted);
    }

    private async Task CreateTestUserFeedsAndArticlesAsync()
    {
        await _webApplicationFactory.CreateTestUserAsync();
        await using var services = _webApplicationFactory.Services.CreateAsyncScope();
        var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.RssFeeds!.Add(new() { Id = 1, Uri = "http://test-feed-1.com" });
        context.RssFeeds!.Add(new() { Id = 2, Uri = "http://test-feed-2.com" });
        context.UserFeeds!.Add(new() { GroupName = "test-group-1", Name = "test-feed-1", RssFeedId = 1, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.UserFeeds!.Add(new() { GroupName = "test-group-1", Name = "test-feed-2", RssFeedId = 2, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.Articles!.Add(new() { Id = 1, ArticleGuid = "test-article-1-guid", Body = "test-article-1-body", Heading = "test-article-1-heading", Published = DateTime.Today.AddHours(-1), RssFeedId = 1 });
        context.Articles!.Add(new() { Id = 2, ArticleGuid = "test-article-2-guid", Body = "test-article-2-body", Heading = "test-article-2-heading", Published = DateTime.Today.AddHours(-2), RssFeedId = 1 });
        context.Articles!.Add(new() { Id = 3, ArticleGuid = "test-article-3-guid", Body = "test-article-3-body", Heading = "test-article-3-heading", Published = DateTime.Today.AddHours(-3), RssFeedId = 1 });
        context.Articles!.Add(new() { Id = 4, ArticleGuid = "test-article-4-guid", Body = "test-article-4-body", Heading = "test-article-4-heading", Published = DateTime.Today.AddHours(-4), RssFeedId = 2 });
        context.UserArticlesRead!.Add(new() { ArticleId = 1, UserAccountId = _webApplicationFactory.TestUser.Id, UserFeedId = 1 });
        await context.SaveChangesAsync();
    }

    private record FeedInfo();

    private record FeedArticle(bool read, int feed, FeedInfo? feedInfo, int story, string heading, string article, string posted);
}