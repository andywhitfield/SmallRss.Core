using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Feeds
{
    public interface IRefreshRssFeeds
    {
        Task<bool> ExecuteAsync(List<RssFeed> feedsToRefresh, CancellationToken stoppingToken);
    }
}