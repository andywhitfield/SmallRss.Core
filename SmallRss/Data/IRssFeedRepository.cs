using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IRssFeedRepository
    {
        Task<IEnumerable<RssFeed>> GetByIdsAsync(IEnumerable<int> rssFeedIds);
    }
}