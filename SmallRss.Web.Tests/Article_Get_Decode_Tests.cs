using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SmallRss.Data;

namespace SmallRss.Web.Tests;

[TestClass]
public class Article_Get_Decode_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    [TestMethod]
    [DataRow(true, "article 1 <b>body</b>")]
    [DataRow(false, "article 1 &lt;b&gt;body&lt;/b&gt;")]
    [DataRow(null, "article 1 &lt;b&gt;body&lt;/b&gt;")]
    public async Task Get_article_and_decode(bool? decodeBody, string expectedBody)
    {
        await CreateTestArticleAsync(decodeBody);

        using var client = await _webApplicationFactory.CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync("/api/article/1");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<ArticleJson>();
        Assert.IsNotNull(responseContent);
        Assert.AreEqual(1, responseContent.Id);
        Assert.AreEqual(expectedBody, responseContent.Body);
        Assert.AreEqual("url://article-1", responseContent.Url);
        Assert.AreEqual("article author", responseContent.Author);
    }

    private async Task CreateTestArticleAsync(bool? decodeBody)
    {
        await _webApplicationFactory.CreateTestUserAsync();
        await using var services = _webApplicationFactory.Services.CreateAsyncScope();
        var context = services.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.RssFeeds!.Add(new() { Id = 1, Uri = "http://test-feed-1.com", DecodeBody = decodeBody });
        context.UserFeeds!.Add(new() { GroupName = "test-group-1", Name = "test-feed-1", RssFeedId = 1, UserAccountId = _webApplicationFactory.TestUser.Id });
        context.Articles!.Add(new() { Id = 1, RssFeedId = 1, ArticleGuid = "article-1", Author = "article author", Body = "article 1 &lt;b&gt;body&lt;/b&gt;", Url = "url://article-1" });
        await context.SaveChangesAsync();
    }

    private record ArticleJson(int Id, string Body, string Url, string Author);
}