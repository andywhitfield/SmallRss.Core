using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmallRss.Models;

namespace SmallRss.Data
{
    public class RssFeedRepository : IRssFeedRepository
    {
        private readonly SqliteDataContext _context;

        public RssFeedRepository(SqliteDataContext context)
        {
            _context = context;
        }

        public Task<RssFeed> GetByIdAsync(int rssFeedId)
        {
            return _context.RssFeeds.FindAsync(rssFeedId).AsTask();
        }

        public Task<List<RssFeed>> GetByIdsAsync(IEnumerable<int> rssFeedIds)
        {
            if (rssFeedIds == null)
                return Task.FromResult(new List<RssFeed>(0));

            var idSet = rssFeedIds.ToHashSet();
            return _context.RssFeeds.Where(r => idSet.Contains(r.Id)).ToListAsync();
        }

        public Task<List<RssFeed>> FindByLastUpdatedSinceAsync(DateTime? lastUpdatedSince)
        {
            if (lastUpdatedSince.GetValueOrDefault(default) == default)
                return _context.RssFeeds.ToListAsync();
            return _context.RssFeeds.Where(r => r.LastUpdated >= lastUpdatedSince).ToListAsync();
        }
    }
}