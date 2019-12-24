using System.Threading;
using System.Threading.Tasks;

namespace SmallRss.Feeds
{
    public interface IRefreshRssFeeds
    {
        Task<bool> ExecuteAsync(CancellationToken stoppingToken);
    }
}