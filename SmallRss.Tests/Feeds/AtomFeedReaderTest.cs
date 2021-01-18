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
    public class AtomFeedReaderTest
    {
        private AtomFeedReader _feedReader;

        public AtomFeedReaderTest()
        {
            _feedReader = new AtomFeedReader(Mock.Of<ILogger<AtomFeedReader>>());
        }

        [Fact]
        public async Task CanReadValidAtomFeed()
        {
            using var fs = new FileStream("feed.atom.xml", FileMode.Open);
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
        [InlineData("<notatomfeed />")]
        [InlineData("<rss version=\"2.0\" />")]
        public void CannotReadInvalidFeed(string xml)
        {
            Assert.False(_feedReader.CanRead(XDocument.Parse(xml)));
        }

        [Fact]
        public async Task ReadEmptyAtomFeed()
        {
            using var fs = new FileStream("feed.atom.empty.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            var readResult = await _feedReader.ReadAsync(validDoc);
            Assert.NotNull(readResult);
            Assert.True(readResult.IsValid);
            Assert.NotNull(readResult.Feed);
            Assert.NotNull(readResult.Articles);
            Assert.Equal(DateTime.ParseExact("2019-12-24T01:42:28Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
            Assert.Equal("https://daringfireball.net/", readResult.Feed.Link);

            Assert.Empty(readResult.Articles);
        }

        [Fact]
        public async Task ReadValidAtomFeed()
        {
            using var fs = new FileStream("feed.atom.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            var readResult = await _feedReader.ReadAsync(validDoc);
            Assert.NotNull(readResult);
            Assert.True(readResult.IsValid);
            Assert.NotNull(readResult.Feed);
            Assert.NotNull(readResult.Articles);
            Assert.Equal(DateTime.ParseExact("2019-12-24T01:42:28Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
            Assert.Equal("https://daringfireball.net/", readResult.Feed.Link);

            Assert.Equal(48, readResult.Articles.Count());

            // just check the first & last
            var article = readResult.Articles.First();
            Assert.Equal("tag:daringfireball.net,2019:/linked//6.36322", article.ArticleGuid);
            Assert.Equal("John Gruber", article.Author);
            Assert.Contains("Looking to the AirPods first", article.Body);
            Assert.Equal("AirPods Pro Bluetooth Latency", article.Heading);
            Assert.Equal(DateTime.ParseExact("2019-12-23T22:51:25Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.Equal("https://stephencoyle.net/airpods-pro", article.Url);

            article = readResult.Articles.Last();
            Assert.Equal("tag:daringfireball.net,2019://1.36238", article.ArticleGuid);
            Assert.Equal("John Gruber", article.Author);
            Assert.Contains("If You Try Sometimes, You Just Might Find, You Get What You Need", article.Body);
            Assert.Equal("★ 16-Inch MacBook Pro First Impressions: Great Keyboard, Outstanding Speakers", article.Heading);
            Assert.Equal(DateTime.ParseExact("2019-11-20T18:42:18Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.Equal("https://daringfireball.net/2019/11/16-inch_macbook_pro_first_impressions", article.Url);
        }

        [Fact]
        public async Task ReadValidAtomFeedWithOddDateFormat()
        {
            using var fs = new FileStream("feed.atom2.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            var readResult = await _feedReader.ReadAsync(validDoc);
            Assert.NotNull(readResult);
            Assert.True(readResult.IsValid);
            Assert.NotNull(readResult.Feed);
            Assert.NotNull(readResult.Articles);
            Assert.Equal(DateTime.ParseExact("2019-12-12T17:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
            Assert.Equal("https://code.visualstudio.com/", readResult.Feed.Link);

            Assert.Equal(20, readResult.Articles.Count());

            // just check the first & last
            var article = readResult.Articles.First();
            Assert.Equal("https://code.visualstudio.com/updates/v1_41", article.ArticleGuid);
            Assert.Equal("Visual Studio Code Team", article.Author);
            Assert.Contains("Visual Studio Code November 2019", article.Body);
            Assert.Equal("Visual Studio Code November 2019", article.Heading);
            Assert.Equal(DateTime.ParseExact("2019-12-12T17:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.Equal("https://code.visualstudio.com/updates/v1_41", article.Url);

            article = readResult.Articles.Last();
            Assert.Equal("https://code.visualstudio.com/blogs/2018/12/04/rich-navigation", article.ArticleGuid);
            Assert.Equal("Jonathan Carter", article.Author);
            Assert.Contains("First look at a rich code navigation experience in Visual Studio", article.Body);
            Assert.Equal("Rich Code Navigation", article.Heading);
            Assert.Equal(DateTime.ParseExact("2018-12-04T00:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.Equal("https://code.visualstudio.com/blogs/2018/12/04/rich-navigation", article.Url);
        }
    }
}