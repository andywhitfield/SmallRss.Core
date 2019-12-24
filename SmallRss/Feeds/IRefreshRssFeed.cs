using System.Threading;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Feeds
{
    public interface IRefreshRssFeed
    {
        Task RefreshAsync(RssFeed rssFeed, CancellationToken cancellationToken);
    }
}