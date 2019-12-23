using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IRssFeedRepository
    {
        Task<List<RssFeed>> GetByIdsAsync(IEnumerable<int> rssFeedIds);
        Task<List<RssFeed>> FindByLastUpdatedSinceAsync(DateTime? lastUpdatedSince);
    }
}