using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IRssFeedRepository
    {
        Task<RssFeed> CreateAsync(string uri);
        Task<RssFeed> GetByIdAsync(int rssFeedId);
        Task<RssFeed> GetByUriAsync(string uri);
        Task<List<RssFeed>> GetByIdsAsync(IEnumerable<int> rssFeedIds);
        Task<List<RssFeed>> FindByLastUpdatedSinceAsync(DateTime? lastUpdatedSince);
        Task RemoveWhereNoUserFeedAsync();
    }
}