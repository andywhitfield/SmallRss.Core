using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.Protected;
using SmallRss.Feeds;
using SmallRss.Models;

namespace SmallRss.Tests.Feeds;

[TestClass]
public class RssFeedImageLocator_from_html_link_tag_Test
{
    private FakeTimeProvider? _timeProvider;

    [TestMethod]
    public async Task Should_set_image_from_html_link_tag()
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        RssFeed rssFeed = new();
        FeedParseResult feedParseResult = new("test", new() { Link = "http://test.url/home" }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.AreEqual("http://test.url/icon.png", rssFeed.ImageUrl);
        Assert.AreEqual(_timeProvider!.GetUtcNow().UtcDateTime, rssFeed.ImageUrlUpdated);
    }

    [TestMethod]
    public async Task Should_set_image_from_html_link_tag_with_absolute_icon()
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        RssFeed rssFeed = new();
        FeedParseResult feedParseResult = new("test", new() { Link = "http://test.url/absolute" }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.AreEqual("http://test.url/absolute/icon.png", rssFeed.ImageUrl);
        Assert.AreEqual(_timeProvider!.GetUtcNow().UtcDateTime, rssFeed.ImageUrlUpdated);
    }

    [TestMethod]
    public async Task Should_not_set_image_when_missing_link_tag()
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        RssFeed rssFeed = new();
        FeedParseResult feedParseResult = new("test", new() { Link = "http://test.url/notag" }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.IsNull(rssFeed.ImageUrl);
        Assert.AreEqual(_timeProvider!.GetUtcNow().UtcDateTime, rssFeed.ImageUrlUpdated);
    }

    [TestMethod]
    public async Task Should_not_set_image_when_link_tag_uri_is_invalid()
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        RssFeed rssFeed = new();
        FeedParseResult feedParseResult = new("test", new() { Link = "http://test.url/badtag" }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.IsNull(rssFeed.ImageUrl);
        Assert.AreEqual(_timeProvider!.GetUtcNow().UtcDateTime, rssFeed.ImageUrlUpdated);
    }

    private RssFeedImageLocator CreateRssFeedImageLocator()
    {
        DateTime now = new(2026, 8, 31, 7, 30, 0, DateTimeKind.Utc);
        _timeProvider = new(now);

        Mock<IHttpClientFactory> mockHttpClientFactory = new();
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage requestMessage, CancellationToken cancellationToken) =>
            {
                if (requestMessage.RequestUri?.AbsoluteUri == "http://test.url/")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("<html><body>Test Site Root</body></html>")
                    };
                }

                if (requestMessage.RequestUri?.AbsoluteUri == "http://test.url/home")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("""
<html>
    <head><link rel="icon" href="/icon.png" /></head>
    <body>Test Site Home Page</body>
</html>
""")
                    };
                }

                if (requestMessage.RequestUri?.AbsoluteUri == "http://test.url/absolute")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("""
<html>
    <head><link rel="icon" href="http://test.url/absolute/icon.png" /></head>
    <body>Test Site Home Page</body>
</html>
""")
                    };
                }

                if (requestMessage.RequestUri?.AbsoluteUri == "http://test.url/notag")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("<html><body>Test Site NoTag Page</body></html>")
                    };
                }

                if (requestMessage.RequestUri?.AbsoluteUri == "http://test.url/badtag")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("""
<html>
    <head><link rel="icon" href="/404.png" /></head>
    <body>Test Site BadTag Page</body>
</html>
""")
                    };
                }

                if (requestMessage.RequestUri?.AbsoluteUri == "http://test.url/icon.png" || requestMessage.RequestUri?.AbsoluteUri == "http://test.url/absolute/icon.png")
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };

                return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
            });
        HttpClient client = new(mockHttpMessageHandler.Object);
        mockHttpClientFactory.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(client);

        return new(Mock.Of<ILogger<RssFeedImageLocator>>(), _timeProvider, mockHttpClientFactory.Object);
    }
}