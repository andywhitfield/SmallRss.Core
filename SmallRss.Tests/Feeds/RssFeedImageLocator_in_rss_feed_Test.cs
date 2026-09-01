using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.Protected;
using SmallRss.Feeds;
using SmallRss.Models;

namespace SmallRss.Tests.Feeds;

[TestClass]
public class RssFeedImageLocator_in_rss_feed_Test
{
    private FakeTimeProvider? _timeProvider;

    [TestMethod]
    [DataRow("")]
    [DataRow("2026-08-24 07:29:59")]
    [DataRow("2026-08-24 00:00:00")]
    [DataRow("2026-08-23 07:30:00")]
    public async Task Given_a_valid_url_in_the_feed_Should_update_rssfeed_as_expected(string lastUpdated)
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        RssFeed rssFeed = new() { ImageUrlUpdated = string.IsNullOrEmpty(lastUpdated) ? null : DateTime.ParseExact(lastUpdated, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal) };
        FeedParseResult feedParseResult = new("test", new() { ImageUrl = "http://test.url/icon.png" }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.AreEqual("http://test.url/icon.png", rssFeed.ImageUrl);
        Assert.AreEqual(_timeProvider!.GetUtcNow().UtcDateTime, rssFeed.ImageUrlUpdated);
    }

    [TestMethod]
    [DataRow("2026-08-31 07:30:00")]
    [DataRow("2026-08-30 00:00:00")]
    [DataRow("2026-08-24 07:30:00")]
    public async Task When_url_has_been_recently_updated_Should_skip_update(string lastUpdated)
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        var imageUrlUpdated = DateTime.ParseExact(lastUpdated, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal);
        RssFeed rssFeed = new() { ImageUrlUpdated = imageUrlUpdated };
        FeedParseResult feedParseResult = new("test", new() { ImageUrl = "http://test.url/icon.png" }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.IsNull(rssFeed.ImageUrl);
        Assert.AreEqual(imageUrlUpdated, rssFeed.ImageUrlUpdated);
    }

    [TestMethod]
    [DataRow("not-a-url")]
    [DataRow("")]
    [DataRow("http://")]
    public async Task Given_invalid_url_in_feed_Should_not_update_rssfeed(string url)
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        RssFeed rssFeed = new();
        FeedParseResult feedParseResult = new("test", new() { ImageUrl = url }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.IsNull(rssFeed.ImageUrl);
        Assert.AreEqual(_timeProvider!.GetUtcNow().DateTime, rssFeed.ImageUrlUpdated);
    }

    [TestMethod]
    public async Task Given_invalid_resource_Should_not_update_rssfeed()
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        RssFeed rssFeed = new();
        FeedParseResult feedParseResult = new("test", new() { ImageUrl = "http://test.url/404" }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.IsNull(rssFeed.ImageUrl);
        Assert.AreEqual(_timeProvider!.GetUtcNow().DateTime, rssFeed.ImageUrlUpdated);
    }

    private RssFeedImageLocator CreateRssFeedImageLocator()
    {
        DateTime now = new(2026, 8, 31, 7, 30, 0, DateTimeKind.Utc);
        _timeProvider = new(now);

        Mock<IHttpClientFactory> mockHttpClientFactory = new();
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage requestMessage, CancellationToken cancellationToken) => new HttpResponseMessage
            {
                StatusCode = requestMessage.RequestUri?.AbsolutePath.EndsWith("404") ?? false ? HttpStatusCode.NotFound : HttpStatusCode.OK
            });
        HttpClient client = new(mockHttpMessageHandler.Object);
        mockHttpClientFactory.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(client);

        return new(Mock.Of<ILogger<RssFeedImageLocator>>(), _timeProvider, mockHttpClientFactory.Object);
    }
}