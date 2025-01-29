using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SmallRss.Feeds;

namespace SmallRss.Tests.Feeds
{
    [TestClass]
    public class AtomFeedReaderTest
    {
        private AtomFeedReader _feedReader;

        [TestInitialize]
        public void Setup()
        {
            _feedReader = new AtomFeedReader(Mock.Of<ILogger<AtomFeedReader>>());
        }

        [TestMethod]
        public async Task CanReadValidAtomFeed()
        {
            using var fs = new FileStream("feed.atom.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            Assert.IsTrue(_feedReader.CanRead(validDoc));
        }

        [TestMethod]
        public void CannotReadEmptyXml()
        {
            Assert.IsFalse(_feedReader.CanRead(new XDocument()));
        }

        [TestMethod]
        public void CannotReadNull()
        {
            Assert.IsFalse(_feedReader.CanRead(null));
        }

        [TestMethod]
        [DataRow("<notatomfeed />")]
        [DataRow("<rss version=\"2.0\" />")]
        public void CannotReadInvalidFeed(string xml)
        {
            Assert.IsFalse(_feedReader.CanRead(XDocument.Parse(xml)));
        }

        [TestMethod]
        public async Task ReadEmptyAtomFeed()
        {
            using var fs = new FileStream("feed.atom.empty.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            var readResult = await _feedReader.ReadAsync(validDoc);
            Assert.IsNotNull(readResult);
            Assert.IsTrue(readResult.IsValid);
            Assert.IsNotNull(readResult.Feed);
            Assert.IsNotNull(readResult.Articles);
            Assert.AreEqual(DateTime.ParseExact("2019-12-24T01:42:28Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
            Assert.AreEqual("https://daringfireball.net/", readResult.Feed.Link);

            Assert.IsFalse(readResult.Articles.Any());
        }

        [TestMethod]
        public async Task ReadValidAtomFeed()
        {
            using var fs = new FileStream("feed.atom.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            var readResult = await _feedReader.ReadAsync(validDoc);
            Assert.IsNotNull(readResult);
            Assert.IsTrue(readResult.IsValid);
            Assert.IsNotNull(readResult.Feed);
            Assert.IsNotNull(readResult.Articles);
            Assert.AreEqual(DateTime.ParseExact("2019-12-24T01:42:28Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
            Assert.AreEqual("https://daringfireball.net/", readResult.Feed.Link);

            Assert.AreEqual(48, readResult.Articles.Count());

            // just check the first & last
            var article = readResult.Articles.First();
            Assert.AreEqual("tag:daringfireball.net,2019:/linked//6.36322", article.ArticleGuid);
            Assert.AreEqual("John Gruber", article.Author);
            StringAssert.Contains(article.Body, "Looking to the AirPods first");
            Assert.AreEqual("AirPods Pro Bluetooth Latency", article.Heading);
            Assert.AreEqual(DateTime.ParseExact("2019-12-23T22:51:25Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.AreEqual("https://stephencoyle.net/airpods-pro", article.Url);

            article = readResult.Articles.Last();
            Assert.AreEqual("tag:daringfireball.net,2019://1.36238", article.ArticleGuid);
            Assert.AreEqual("John Gruber", article.Author);
            StringAssert.Contains(article.Body, "If You Try Sometimes, You Just Might Find, You Get What You Need");
            Assert.AreEqual("★ 16-Inch MacBook Pro First Impressions: Great Keyboard, Outstanding Speakers", article.Heading);
            Assert.AreEqual(DateTime.ParseExact("2019-11-20T18:42:18Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.AreEqual("https://daringfireball.net/2019/11/16-inch_macbook_pro_first_impressions", article.Url);
        }

        [TestMethod]
        public async Task ReadValidAtomFeedWithOddDateFormat()
        {
            using var fs = new FileStream("feed.atom2.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            var readResult = await _feedReader.ReadAsync(validDoc);
            Assert.IsNotNull(readResult);
            Assert.IsTrue(readResult.IsValid);
            Assert.IsNotNull(readResult.Feed);
            Assert.IsNotNull(readResult.Articles);
            Assert.AreEqual(DateTime.ParseExact("2019-12-12T17:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
            Assert.AreEqual("https://code.visualstudio.com/", readResult.Feed.Link);

            Assert.AreEqual(20, readResult.Articles.Count());

            // just check the first & last
            var article = readResult.Articles.First();
            Assert.AreEqual("https://code.visualstudio.com/updates/v1_41", article.ArticleGuid);
            Assert.AreEqual("Visual Studio Code Team", article.Author);
            StringAssert.Contains(article.Body, "Visual Studio Code November 2019");
            Assert.AreEqual("Visual Studio Code November 2019", article.Heading);
            Assert.AreEqual(DateTime.ParseExact("2019-12-12T17:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.AreEqual("https://code.visualstudio.com/updates/v1_41", article.Url);

            article = readResult.Articles.Last();
            Assert.AreEqual("https://code.visualstudio.com/blogs/2018/12/04/rich-navigation", article.ArticleGuid);
            Assert.AreEqual("Jonathan Carter", article.Author);
            StringAssert.Contains(article.Body, "First look at a rich code navigation experience in Visual Studio");
            Assert.AreEqual("Rich Code Navigation", article.Heading);
            Assert.AreEqual(DateTime.ParseExact("2018-12-04T00:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.AreEqual("https://code.visualstudio.com/blogs/2018/12/04/rich-navigation", article.Url);
        }
    }
}