using SmallRss.Models;

namespace SmallRss.Feeds;

public interface IRssFeedImageLocator
{
    Task SetImageUrlAsync(RssFeed rssFeed, FeedParseResult feedParseResult, CancellationToken cancellationToken);
}