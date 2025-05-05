using SmallRss.Models;

namespace SmallRss.Feeds;

public interface IRefreshRssFeeds
{
    Task ExecuteAsync(List<RssFeed> feedsToRefresh, CancellationToken stoppingToken);
}