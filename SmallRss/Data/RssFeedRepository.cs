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

        public RssFeedRepository(SqliteDataContext context) => _context = context;

        public async Task<RssFeed> CreateAsync(string uri)
        {
            var feed = await GetByUriAsync(uri);
            if (feed != null)
                throw new ArgumentException($"RSS feed already exists with uri: [{uri}]");
            var added = await _context.RssFeeds!.AddAsync(new RssFeed { Uri = uri });
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public Task<RssFeed?> GetByIdAsync(int rssFeedId) =>
            _context.RssFeeds!.FindAsync(rssFeedId).AsTask();

        public Task<RssFeed?> GetByUriAsync(string uri) =>
            _context.RssFeeds!.FirstOrDefaultAsync(rf => rf.Uri == uri);

        public Task<List<RssFeed>> GetByIdsAsync(IEnumerable<int> rssFeedIds)
        {
            if (rssFeedIds == null)
                return Task.FromResult(new List<RssFeed>(0));

            var idSet = rssFeedIds.ToHashSet();
            return _context.RssFeeds!.Where(r => idSet.Contains(r.Id)).ToListAsync();
        }

        public Task<List<RssFeed>> FindByLastUpdatedSinceAsync(DateTime? lastUpdatedSince)
        {
            if (lastUpdatedSince.GetValueOrDefault(default) == default)
                return _context.RssFeeds!.ToListAsync();
            return _context.RssFeeds!.Where(r => r.LastUpdated >= lastUpdatedSince).ToListAsync();
        }

        public async Task RemoveWhereNoUserFeedAsync()
        {
            _context.RssFeeds!.RemoveRange(_context.RssFeeds.FromSqlRaw("select * from RssFeeds where Id not in (select RssFeedId from UserFeeds)"));
            await _context.SaveChangesAsync();
        }

        public async Task<RssFeed?> UpdateDecodeBodyAsync(int rssFeedId, bool decodeBody)
        {
            var rssFeed = await GetByIdAsync(rssFeedId);
            if (rssFeed == null)
                return null;
            
            if ((rssFeed.DecodeBody ?? false) != decodeBody)
            {
                rssFeed.DecodeBody = decodeBody;
                await _context.SaveChangesAsync();
            }

            return rssFeed;
        }
    }
}