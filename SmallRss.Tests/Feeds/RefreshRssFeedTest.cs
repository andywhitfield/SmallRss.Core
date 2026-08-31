using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SmallRss.Data;
using SmallRss.Feeds;
using SmallRss.Models;

namespace SmallRss.Tests.Feeds;

[TestClass]
public class RefreshRssFeedTest
{
    [TestMethod]
    public async Task Can_refresh_feed()
    {
        Mock<IHttpClientFactory> mockHttpClientFactory = new();
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(await File.ReadAllTextAsync("feed.rss.xml")),
            });

        HttpClient client = new(mockHttpMessageHandler.Object);
        mockHttpClientFactory.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(client);

        Mock<IArticleRepository> articleRepository = new();
        Mock<IRssFeedImageLocator> rssFeedImageLocatorMock = new();
        rssFeedImageLocatorMock
            .Setup(l => l.SetImageUrlAsync(It.IsAny<RssFeed>(), It.IsAny<FeedParseResult>(), It.IsAny<CancellationToken>()))
            .Callback((RssFeed rssFeed, FeedParseResult feedParseResult, CancellationToken ct) =>
            {
                rssFeed.ImageUrl = feedParseResult.Feed.ImageUrl;
                rssFeed.ImageUrlUpdated = DateTime.UtcNow;
            });
        
        RefreshRssFeed refreshRssFeed = new(Mock.Of<ILogger<RefreshRssFeed>>(), mockHttpClientFactory.Object, new FeedParser(Mock.Of<ILogger<FeedParser>>(), [new RssFeedReader(Mock.Of<ILogger<RssFeedReader>>())]), articleRepository.Object, rssFeedImageLocatorMock.Object);
        RssFeed feed = new()
        {
            Uri = "http://test.rss/feed"
        };
        
        var result = await refreshRssFeed.RefreshAsync(feed, CancellationToken.None);
        Assert.IsTrue(result);
        Assert.AreEqual("https://9to5mac.com", feed.Link);
        Assert.AreEqual("https://9to5mac.com/wp-content/uploads/sites/6/2019/10/cropped-cropped-mac1-1.png?w=32", feed.ImageUrl);
        Assert.AreEqual(new DateTime(2019, 12, 24, 1, 21, 58, DateTimeKind.Utc), feed.LastUpdated);
        Assert.IsTrue(feed.LastRefreshSuccess);
        Assert.AreEqual("", feed.LastRefreshMessage);
        articleRepository.Verify(ar => ar.CreateAsync(feed, It.IsAny<Article>()), Times.Exactly(100));
    }
}