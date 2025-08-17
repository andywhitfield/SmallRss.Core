using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SmallRss.Data;

namespace SmallRss.Web.Tests;

[TestClass]
public class Article_Get_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    [TestMethod]
    public async Task Get_unknown_article_returns_404()
    {
        await CreateTestArticlesAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync("/api/article/100");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    [DataRow(1, "article 1", "url://article-1", "")]
    [DataRow(2, "article 2", "", "article 2 author")]
    public async Task Get_article(int articleId, string expectedBody, string expectedUrl, string expectedAuthor)
    {
        await CreateTestArticlesAsync();

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync("/api/article/" + articleId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<ArticleJson>();
        Assert.IsNotNull(responseContent);
        Assert.AreEqual(articleId, responseContent.Id);
        Assert.AreEqual(expectedBody, responseContent.Body);
        Assert.AreEqual(expectedUrl, responseContent.Url);
        Assert.AreEqual(expectedAuthor, responseContent.Author);
    }

    private async Task CreateTestArticlesAsync()
    {
        await _webApplicationFactory.CreateTestUserAsync();
        await using var services = _webApplicationFactory.Services.CreateAsyncScope();
        var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.RssFeeds!.Add(new() { Id = 1, Uri = "http://test-feed-1.com" });
        context.UserFeeds!.Add(new() { GroupName = "test-group-1", Name = "test-feed-1", RssFeedId = 1, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.Articles!.Add(new() { Id = 1, RssFeedId = 1, ArticleGuid = "article-1", Body = "article 1", Url = "url://article-1" });
        context.Articles!.Add(new() { Id = 2, RssFeedId = 1, ArticleGuid = "article-2", Body = "article 2", Author = "article 2 author" });
        await context.SaveChangesAsync();
    }

    private record ArticleJson(int Id, string Body, string Url, string Author);
}