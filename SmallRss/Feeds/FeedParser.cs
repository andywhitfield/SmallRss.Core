using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace SmallRss.Feeds
{
    public class FeedParser : IFeedParser
    {
        private readonly ILogger<FeedParser> _logger;
        private readonly IEnumerable<IFeedReader> _readers;

        public FeedParser(ILogger<FeedParser> logger, IEnumerable<IFeedReader> readers)
        {
            _logger = logger;
            _readers = readers;
        }

        public Task<FeedParseResult> ParseAsync(Stream feedContent, CancellationToken cancellationToken)
        {
            try
            {
                return ParseInternalAsync(feedContent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing feed content");
                return Task.FromResult(FeedParseResult.FailureResult);
            }
        }

        private async Task<FeedParseResult> ParseInternalAsync(Stream feedContent, CancellationToken cancellationToken)
        {
            var doc = await XDocument.LoadAsync(feedContent, LoadOptions.None, cancellationToken);
            var feedReader = _readers.FirstOrDefault(r => r.CanRead(doc));
            if (feedReader == null)
            {
                _logger.LogError($"No IFeedReader registered that can handle feed content type: {doc}");
                return FeedParseResult.FailureResult;
            }
            return await feedReader.ReadAsync(doc);
        }
    }
}