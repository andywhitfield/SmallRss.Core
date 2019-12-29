using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Feeds
{
    public class RssFeedReader : IFeedReader
    {
        private const string RssRootElementName = "rss";
        private const string RssVersionAttributeName = "version";

        private readonly XNamespace nsRssContent = "http://purl.org/rss/1.0/modules/content/";
        private readonly XNamespace nsAtom = "http://www.w3.org/2005/Atom";
        private readonly XNamespace nsDc = "http://purl.org/dc/elements/1.1/";

        private readonly ILogger<RssFeedReader> _logger;

        public RssFeedReader(ILogger<RssFeedReader> logger)
        {
            _logger = logger;
        }

        public bool CanRead(XDocument doc)
        {
            return
                (doc?.Root?.Name.LocalName.Equals(RssRootElementName, StringComparison.OrdinalIgnoreCase) ?? false) &&
                (doc?.Root?.Attribute(RssVersionAttributeName)?.Value.Equals("2.0") ?? false);
        }

        public Task<FeedParseResult> ReadAsync(XDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            _logger.LogTrace("Parsing RSS feed");

            var feed = new RssFeed();

            var channel = doc.Root.Element("channel");
            if (channel == null)
            {
                _logger.LogWarning("RSS feed has no channel element, badly formed feed, returning failure result");
                return Task.FromResult(FeedParseResult.FailureResult);
            }

            var feedTitle = channel.Element("title")?.Value ?? channel.Element("description")?.Value ?? string.Empty;
            feed.Link = channel.Element("link")?.Value;

            feed.LastUpdated = channel.Elements("pubDate").FirstOrDefault()?.Value.ToDateTime();
            if (feed.LastUpdated == null)
            {
                feed.LastUpdated = channel.Element("lastBuildDate")?.Value.ToDateTime();
                if (feed.LastUpdated == null)
                    feed.LastUpdated = DateTime.UtcNow;
            }

            var articles = channel.Elements("item").Select(ReadFeedItem).Where(e => e != null).ToList();
            var latestArticle = articles.Max(a => a.Published ?? DateTime.MinValue);
            if (latestArticle > feed.LastUpdated)
                feed.LastUpdated = latestArticle;
            
            return Task.FromResult(new FeedParseResult(feedTitle, feed, articles));
        }

        private Article ReadFeedItem(XElement item)
        {
            var article = new Article();

            article.ArticleGuid = item.Element("guid")?.Value ?? item.Element("link")?.Value ?? item.Element("title")?.Value;
            if (string.IsNullOrEmpty(article.ArticleGuid))
            {
                _logger.LogWarning($"Feed item does not have a guid (or link or title) - badly formed feed, cannot add article. Item: {item}");
                return null;
            }

            article.Heading = item.Element("title")?.Value;
            article.Published = item.Element("pubDate")?.Value.ToDateTime() ?? item.Element(XName.Get("updated", nsAtom.NamespaceName))?.Value.ToDateTime() ?? DateTime.UtcNow;
            article.Author = item.Element("author")?.Value ?? item.Element(XName.Get("creator", nsDc.NamespaceName))?.Value;
            article.Body = item.Element(XName.Get("encoded", nsRssContent.NamespaceName))?.Value ?? item.Element("description")?.Value;
            article.Url = item.Element("link")?.Value;

            _logger.LogTrace($"Parsed feed item {article.ArticleGuid}");

            return article;
        }
    }
}