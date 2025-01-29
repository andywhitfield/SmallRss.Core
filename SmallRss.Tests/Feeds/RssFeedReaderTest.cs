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
    public class RssFeedReaderTest
    {
        private RssFeedReader _feedReader;

        public RssFeedReaderTest()
        {
            _feedReader = new RssFeedReader(Mock.Of<ILogger<RssFeedReader>>());
        }

        [TestMethod]
        public async Task CanReadValidRssFeed()
        {
            using var fs = new FileStream("feed.rss.xml", FileMode.Open);
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
        [DataRow("<notrssfeed />")]
        [DataRow("<feed />")]
        [DataRow("<rss />")]
        [DataRow("<rss version=\"\" />")]
        [DataRow("<rss version=\"something\" />")]
        [DataRow("<rss version=\"1\" />")]
        [DataRow("<rss version=\"2\" />")]
        [DataRow("<rss version=\"3\" />")]
        public void CannotReadInvalidFeed(string xml)
        {
            Assert.IsFalse(_feedReader.CanRead(XDocument.Parse(xml)));
        }

        [TestMethod]
        public async Task ReadValidRssFeed()
        {
            using var fs = new FileStream("feed.rss.xml", FileMode.Open);
            var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
            var readResult = await _feedReader.ReadAsync(validDoc);
            Assert.IsNotNull(readResult);
            Assert.IsTrue(readResult.IsValid);
            Assert.IsNotNull(readResult.Feed);
            Assert.IsNotNull(readResult.Articles);
            Assert.AreEqual(DateTime.ParseExact("2019-12-24T01:21:58Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
            Assert.AreEqual("https://9to5mac.com", readResult.Feed.Link);

            Assert.AreEqual(100, readResult.Articles.Count());

            // just check the first & last
            var article = readResult.Articles.First();
            Assert.AreEqual("https://9to5mac.com/?p=625764", article.ArticleGuid);
            Assert.AreEqual("Zac Hall", article.Author);
            StringAssert.Contains(article.Body, "Uh-oh! You finished all your Christmas shopping for your kids");
            Assert.AreEqual("It’s never too late for these parent-recommended tech gifts for kids", article.Heading);
            Assert.AreEqual(DateTime.ParseExact("2019-12-23T22:19:56Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.AreEqual("https://9to5mac.com/2019/12/23/tech-gift-guides-kids/", article.Url);

            article = readResult.Articles.Last();
            Assert.AreEqual("https://9to5mac.com/?p=624696", article.ArticleGuid);
            Assert.AreEqual("Benjamin Mayo", article.Author);
            StringAssert.Contains(article.Body, "Just before the holidays, the Pixelmator Pro team have pushed out another update");
            Assert.AreEqual("Pixelmator Pro’s new ML-powered ‘Super Resolution’ mode enlarges images while maintaining sharpness", article.Heading);
            Assert.AreEqual(DateTime.ParseExact("2019-12-17T15:03:07Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
            Assert.AreEqual("https://9to5mac.com/2019/12/17/pixelmator-pro-super-resolution/", article.Url);
        }
    }
}