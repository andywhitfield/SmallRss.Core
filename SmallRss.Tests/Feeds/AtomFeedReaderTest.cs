using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using SmallRss.Feeds;

[assembly: Parallelize]

namespace SmallRss.Tests.Feeds;

[TestClass]
public class AtomFeedReaderTest
{
    private readonly AtomFeedReader? _feedReader;

    public AtomFeedReaderTest()
        => _feedReader = new(Mock.Of<ILogger<AtomFeedReader>>());

    [TestMethod]
    public async Task CanReadValidAtomFeed()
    {
        using var fs = new FileStream("feed.atom.xml", FileMode.Open);
        var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
        Assert.IsTrue(_feedReader!.CanRead(validDoc));
    }

    [TestMethod]
    public void CannotReadEmptyXml()
    {
        Assert.IsFalse(_feedReader!.CanRead(new()));
    }

    [TestMethod]
    public void CannotReadNull()
    {
        Assert.IsFalse(_feedReader!.CanRead(null));
    }

    [TestMethod]
    [DataRow("<notatomfeed />")]
    [DataRow("<rss version=\"2.0\" />")]
    public void CannotReadInvalidFeed(string xml)
    {
        Assert.IsFalse(_feedReader!.CanRead(XDocument.Parse(xml)));
    }

    [TestMethod]
    public async Task ReadEmptyAtomFeed()
    {
        await using FileStream fs = new("feed.atom.empty.xml", FileMode.Open);
        var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
        var readResult = await _feedReader!.ReadAsync(validDoc);
        Assert.IsNotNull(readResult);
        Assert.IsTrue(readResult.IsValid);
        Assert.IsNotNull(readResult.Feed);
        Assert.IsNotNull(readResult.Articles);
        Assert.AreEqual(DateTime.ParseExact("2019-12-24T01:42:28Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
        Assert.AreEqual("https://daringfireball.net/", readResult.Feed.Link);
        Assert.IsNull(readResult.Feed.ImageUrl);

        Assert.IsEmpty(readResult.Articles);
    }

    [TestMethod]
    public async Task ReadValidAtomFeed()
    {
        using FileStream fs = new("feed.atom.xml", FileMode.Open);
        var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
        var readResult = await _feedReader!.ReadAsync(validDoc);
        Assert.IsNotNull(readResult);
        Assert.IsTrue(readResult.IsValid);
        Assert.IsNotNull(readResult.Feed);
        Assert.IsNotNull(readResult.Articles);
        Assert.AreEqual(DateTime.ParseExact("2019-12-24T01:42:28Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
        Assert.AreEqual("https://daringfireball.net/", readResult.Feed.Link);
        Assert.AreEqual("https://daringfireball.net/graphics/favicon.ico?v=005", readResult.Feed.ImageUrl);

        Assert.HasCount(48, readResult.Articles);

        // just check the first & last
        var article = readResult.Articles.First();
        Assert.AreEqual("tag:daringfireball.net,2019:/linked//6.36322", article.ArticleGuid);
        Assert.AreEqual("John Gruber", article.Author);
        Assert.Contains("Looking to the AirPods first", article.Body ?? "");
        Assert.AreEqual("AirPods Pro Bluetooth Latency", article.Heading);
        Assert.AreEqual(DateTime.ParseExact("2019-12-23T22:51:25Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
        Assert.AreEqual("https://stephencoyle.net/airpods-pro", article.Url);

        article = readResult.Articles.Last();
        Assert.AreEqual("tag:daringfireball.net,2019://1.36238", article.ArticleGuid);
        Assert.AreEqual("John Gruber", article.Author);
        Assert.Contains("If You Try Sometimes, You Just Might Find, You Get What You Need", article.Body ?? "");
        Assert.AreEqual("★ 16-Inch MacBook Pro First Impressions: Great Keyboard, Outstanding Speakers", article.Heading);
        Assert.AreEqual(DateTime.ParseExact("2019-11-20T18:42:18Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
        Assert.AreEqual("https://daringfireball.net/2019/11/16-inch_macbook_pro_first_impressions", article.Url);
    }

    [TestMethod]
    public async Task ReadValidAtomFeedWithOddDateFormat()
    {
        await using FileStream fs = new("feed.atom2.xml", FileMode.Open);
        var validDoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
        var readResult = await _feedReader!.ReadAsync(validDoc);
        Assert.IsNotNull(readResult);
        Assert.IsTrue(readResult.IsValid);
        Assert.IsNotNull(readResult.Feed);
        Assert.IsNotNull(readResult.Articles);
        Assert.AreEqual(DateTime.ParseExact("2019-12-12T17:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), readResult.Feed.LastUpdated);
        Assert.AreEqual("https://code.visualstudio.com/", readResult.Feed.Link);
        Assert.AreEqual("https://code.visualstudio.com/opengraphimg/opengraph-home.png", readResult.Feed.ImageUrl);

        Assert.HasCount(20, readResult.Articles);

        // just check the first & last
        var article = readResult.Articles.First();
        Assert.AreEqual("https://code.visualstudio.com/updates/v1_41", article.ArticleGuid);
        Assert.AreEqual("Visual Studio Code Team", article.Author);
        Assert.Contains("Visual Studio Code November 2019", article.Body ?? "");
        Assert.AreEqual("Visual Studio Code November 2019", article.Heading);
        Assert.AreEqual(DateTime.ParseExact("2019-12-12T17:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
        Assert.AreEqual("https://code.visualstudio.com/updates/v1_41", article.Url);

        article = readResult.Articles.Last();
        Assert.AreEqual("https://code.visualstudio.com/blogs/2018/12/04/rich-navigation", article.ArticleGuid);
        Assert.AreEqual("Jonathan Carter", article.Author);
        Assert.Contains("First look at a rich code navigation experience in Visual Studio", article.Body ?? "");
        Assert.AreEqual("Rich Code Navigation", article.Heading);
        Assert.AreEqual(DateTime.ParseExact("2018-12-04T00:00:00Z", "yyyy-MM-dd'T'HH:mm:ssZ", null), article.Published);
        Assert.AreEqual("https://code.visualstudio.com/blogs/2018/12/04/rich-navigation", article.Url);
    }
}