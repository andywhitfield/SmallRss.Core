using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace SmallRss.Feeds
{
    public class RssFeedReader : IFeedReader
    {
        private const string RssRootElementName = "rss";
        private const string RssVersionAttributeName = "version";

        private readonly ILogger<RssFeedReader> _logger;

        public RssFeedReader(ILogger<RssFeedReader> logger)
        {
            _logger = logger;
        }

        public bool CanRead(XDocument doc)
        {
            return doc.Root.Name.LocalName.Contains(RssRootElementName) && doc.Root.Attribute(RssVersionAttributeName).Value == "2.0";
        }

        public Task<FeedParseResult> ReadAsync(XDocument doc)
        {
            _logger.LogTrace("Parsing RSS feed");
            return Task.FromResult(new FeedParseResult());
        }
    }
}