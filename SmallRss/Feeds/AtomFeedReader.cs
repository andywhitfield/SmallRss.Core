using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Feeds;

public class AtomFeedReader(ILogger<AtomFeedReader> logger) : IFeedReader
{
    private const string AtomRootElementName = "feed";
    private readonly XNamespace ns = "http://www.w3.org/2005/Atom";

    public bool CanRead(XDocument? doc)
        => doc?.Root?.Name.LocalName.Equals(AtomRootElementName, StringComparison.OrdinalIgnoreCase) ?? false;

    public Task<FeedParseResult> ReadAsync(XDocument? doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        logger.LogTrace("Parsing Atom feed");

        RssFeed feed = new();

        var channel = doc.Root;
        var feedTitle = channel?.Element(ns + "title")?.Value ?? channel?.Element(ns + "id")?.Value ?? string.Empty;
        feed.Link = channel?.Element(ns + "author")?.Element(ns + "uri")?.Value;
        if (feed.Link == null)
        {
            var linkNode =
                channel?.Elements(ns + "link").SingleOrDefault(x => x.HasAttributes && x.Attribute("rel") == null) ??
                channel?.Elements(ns + "link").SingleOrDefault(x => x.HasAttributes && x.Attribute("rel") != null && x.Attribute("rel")?.Value == "alternate");

            feed.Link = linkNode?.Attribute("href")?.Value;
        }
        feed.ImageUrl = channel?.Element(ns + "icon")?.Value?.Trim() ?? channel?.Element(ns + "logo")?.Value?.Trim();
        feed.LastUpdated = channel?.Element(ns + "updated")?.Value.ToDateTime() ?? DateTime.UtcNow;

        var articles = channel?.Elements(ns + "entry").Select(ReadFeedEntry).Where(e => e != null).Select(a => a!);
        var latestArticle = articles?.Max(a => a.Published) ?? DateTime.MinValue;
        if (latestArticle > feed.LastUpdated)
            feed.LastUpdated = latestArticle;

        return Task.FromResult(new FeedParseResult(feedTitle, feed, articles ?? []));
    }

    private Article? ReadFeedEntry(XElement entry)
    {
        Article article = new()
        {
            ArticleGuid = entry.Element(ns + "id")?.Value
        };

        if (string.IsNullOrEmpty(article.ArticleGuid))
        {
            logger.LogWarning("Feed entry does not have an ID - badly formed feed, cannot add article. Entry: {Entry}", entry);
            return null;
        }

        article.Heading = entry.Element(ns + "title")?.Value;
        article.Published = entry.Element(ns + "updated")?.Value.ToDateTime() ?? DateTime.UtcNow;
        article.Author = entry.Element(ns + "author")?.Element(ns + "name")?.Value;
        article.Body = entry.Element(ns + "content")?.Value ?? entry.Element(ns + "summary")?.Value;

        var linkElements = entry.Elements(ns + "link");
        var linkElement = linkElements.SingleOrDefault(x =>
            x.HasAttributes && x.Attribute("rel") != null && x.Attribute("rel")?.Value == "alternate"
        ) ?? linkElements.FirstOrDefault();
        article.Url = linkElement?.Attribute("href")?.Value;

        logger.LogTrace("Parsed feed entry {ArticleGuid}", article.ArticleGuid);

        return article;
    }
}