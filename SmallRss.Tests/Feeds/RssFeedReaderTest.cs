using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using SmallRss.Feeds;
using Xunit;

namespace SmallRss.Tests.Feeds
{
    public class RssFeedReaderTest
    {
        private RssFeedReader _feedReader;

        public RssFeedReaderTest()
        {
            _feedReader = new RssFeedReader(Mock.Of<ILogger<RssFeedReader>>());
        }

        [Fact]
        public async Task CanReadValidRssFeed()
        {
            using var fs = new FileStream("feed.rss.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            Assert.True(_feedReader.CanRead(validDoc));
        }

        [Fact]
        public void CannotReadEmptyXml()
        {
            Assert.False(_feedReader.CanRead(new XDocument()));
        }

        [Fact]
        public void CannotReadNull()
        {
            Assert.False(_feedReader.CanRead(null));
        }

        [Theory]
        [InlineData("<notrssfeed />")]
        [InlineData("<feed />")]
        [InlineData("<rss />")]
        [InlineData("<rss version=\"\" />")]
        [InlineData("<rss version=\"something\" />")]
        [InlineData("<rss version=\"1\" />")]
        [InlineData("<rss version=\"2\" />")]
        [InlineData("<rss version=\"3\" />")]
        public void CannotReadInvalidFeed(string xml)
        {
            Assert.False(_feedReader.CanRead(XDocument.Parse(xml)));
        }

        [Fact]
        public async Task ReadValidRssFeed()
        {
            using var fs = new FileStream("feed.rss.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            var readResult = await _feedReader.ReadAsync(validDoc);
            Assert.NotNull(readResult);
            Assert.True(readResult.IsValid);
            Assert.NotNull(readResult.Feed);
            Assert.NotNull(readResult.Articles);
            Assert.Equal(DateTime.ParseExact("2019-12-24T01:21:58Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
            Assert.Equal("https://9to5mac.com", readResult.Feed.Link);

            Assert.Equal(100, readResult.Articles.Count());

            // just check the first & last
            var article = readResult.Articles.First();
            Assert.Equal("https://9to5mac.com/?p=625764", article.ArticleGuid);
            Assert.Equal("Zac Hall", article.Author);
            Assert.Contains("Uh-oh! You finished all your Christmas shopping for your kids", article.Body);
            Assert.Equal("It’s never too late for these parent-recommended tech gifts for kids", article.Heading);
            Assert.Equal(DateTime.ParseExact("2019-12-23T22:19:56Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.Equal("https://9to5mac.com/2019/12/23/tech-gift-guides-kids/", article.Url);

            article = readResult.Articles.Last();
            Assert.Equal("https://9to5mac.com/?p=624696", article.ArticleGuid);
            Assert.Equal("Benjamin Mayo", article.Author);
            Assert.Contains("Just before the holidays, the Pixelmator Pro team have pushed out another update", article.Body);
            Assert.Equal("Pixelmator Pro’s new ML-powered ‘Super Resolution’ mode enlarges images while maintaining sharpness", article.Heading);
            Assert.Equal(DateTime.ParseExact("2019-12-17T15:03:07Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.Equal("https://9to5mac.com/2019/12/17/pixelmator-pro-super-resolution/", article.Url);
        }
    }
}