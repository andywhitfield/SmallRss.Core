using System.Threading;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Feeds
{
    public interface IRefreshRssFeed
    {
        Task<bool> RefreshAsync(RssFeed rssFeed, CancellationToken cancellationToken);
    }
}