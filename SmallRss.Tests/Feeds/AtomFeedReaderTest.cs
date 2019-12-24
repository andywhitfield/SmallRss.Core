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
    }
}