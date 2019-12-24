using System.IO;
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
    }
}