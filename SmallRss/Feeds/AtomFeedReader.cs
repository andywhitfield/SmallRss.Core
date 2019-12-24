using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace SmallRss.Feeds
{
    public class AtomFeedReader : IFeedReader
    {
        private const string AtomRootElementName = "feed";

        private readonly ILogger<AtomFeedReader> _logger;

        public AtomFeedReader(ILogger<AtomFeedReader> logger)
        {
            _logger = logger;
        }

        public bool CanRead(XDocument doc)
        {
            return doc?.Root?.Name.LocalName.Equals(AtomRootElementName, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public Task<FeedParseResult> ReadAsync(XDocument doc)
        {
            _logger.LogTrace("Parsing Atom feed");
            return Task.FromResult(new FeedParseResult());
        }
    }
}