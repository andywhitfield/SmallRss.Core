using System.Threading;
using System.Threading.Tasks;

namespace SmallRss.Feeds
{
    public interface IRefreshRssFeeds
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}