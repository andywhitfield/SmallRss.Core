using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.Protected;
using SmallRss.Feeds;
using SmallRss.Models;

namespace SmallRss.Tests.Feeds;

[TestClass]
public class RssFeedImageLocator_from_favicon_Test
{
    private FakeTimeProvider? _timeProvider;

    [TestMethod]
    public async Task Should_set_image_from_favicon()
    {
        var rssFeedImageLocator = CreateRssFeedImageLocator();
        RssFeed rssFeed = new();
        FeedParseResult feedParseResult = new("test", new() { Link = "http://test.url/home" }, []);
        await rssFeedImageLocator.SetImageUrlAsync(rssFeed, feedParseResult, CancellationToken.None);

        Assert.AreEqual("http://test.url/favicon.ico", rssFeed.ImageUrl);
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
                if (requestMessage.RequestUri?.AbsoluteUri == "http://test.url/home")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("<html><body>Test Site Home Page</body></html>")
                    };
                }

                if (requestMessage.RequestUri?.AbsoluteUri == "http://test.url/favicon.ico")
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };

                return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
            });
        HttpClient client = new(mockHttpMessageHandler.Object);
        mockHttpClientFactory.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(client);

        return new(Mock.Of<ILogger<RssFeedImageLocator>>(), _timeProvider, mockHttpClientFactory.Object);
    }
}