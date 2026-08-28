using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Feeds;

public class RssFeedReader(ILogger<RssFeedReader> logger) : IFeedReader
{
    private const string RssRootElementName = "rss";
    private const string RssVersionAttributeName = "version";

    private readonly XNamespace nsRssContent = "http://purl.org/rss/1.0/modules/content/";
    private readonly XNamespace nsAtom = "http://www.w3.org/2005/Atom";
    private readonly XNamespace nsDc = "http://purl.org/dc/elements/1.1/";

    public bool CanRead(XDocument? doc)
        => (doc?.Root?.Name.LocalName.Equals(RssRootElementName, StringComparison.OrdinalIgnoreCase) ?? false) &&
            (doc?.Root?.Attribute(RssVersionAttributeName)?.Value.Equals("2.0") ?? false);

    public Task<FeedParseResult> ReadAsync(XDocument? doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        logger.LogTrace("Parsing RSS feed");

        RssFeed feed = new();

        var channel = doc.Root?.Element("channel");
        if (channel == null)
        {
            logger.LogWarning("RSS feed has no channel element, badly formed feed, returning failure result");
            return Task.FromResult(FeedParseResult.FailureResult);
        }

        var feedTitle = channel.Element("title")?.Value ?? channel.Element("description")?.Value ?? "";
        feed.Link = channel.Element("link")?.Value;
        feed.ImageUrl = channel.Element("image")?.Element("url")?.Value?.Trim();

        feed.LastUpdated =
            channel.Elements("pubDate").FirstOrDefault()?.Value.ToDateTime() ??
            channel.Element("lastBuildDate")?.Value.ToDateTime();
        var articles = channel.Elements("item").Select(ReadFeedItem).Where(e => e != null).Select(e => e!);
        var latestArticle = articles.Any() ? articles.Max(a => a.Published ?? DateTime.MinValue) : DateTime.UtcNow;
        if (feed.LastUpdated == null || latestArticle > feed.LastUpdated)
            feed.LastUpdated = latestArticle;

        return Task.FromResult(new FeedParseResult(feedTitle, feed, articles));
    }

    private Article? ReadFeedItem(XElement item)
    {
        Article article = new()
        {
            ArticleGuid = item.Element("guid")?.Value ?? item.Element("link")?.Value ?? item.Element("title")?.Value
        };

        if (string.IsNullOrEmpty(article.ArticleGuid))
        {
            logger.LogWarning("Feed item does not have a guid (or link or title) - badly formed feed, cannot add article. Item: {Item}", item);
            return null;
        }

        article.Heading = item.Element("title")?.Value;
        article.Published = item.Element("pubDate")?.Value.ToDateTime() ?? item.Element(XName.Get("updated", nsAtom.NamespaceName))?.Value.ToDateTime() ?? DateTime.UtcNow;
        article.Author = item.Element("author")?.Value ?? item.Element(XName.Get("creator", nsDc.NamespaceName))?.Value;
        article.Body = item.Element(XName.Get("encoded", nsRssContent.NamespaceName))?.Value ?? item.Element("description")?.Value;
        article.Url = item.Element("link")?.Value;

        logger.LogTrace("Parsed feed item {ArticleGuid}", article.ArticleGuid);

        return article;
    }
}