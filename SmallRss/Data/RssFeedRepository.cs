using Microsoft.EntityFrameworkCore;
using SmallRss.Models;

namespace SmallRss.Data;

public class RssFeedRepository(SqliteDataContext context)
    : IRssFeedRepository
{
    public async Task<RssFeed> CreateAsync(string uri)
    {
        var feed = await GetByUriAsync(uri);
        if (feed != null)
            throw new ArgumentException($"RSS feed already exists with uri: [{uri}]");
        var added = await context.RssFeeds!.AddAsync(new RssFeed { Uri = uri });
        await context.SaveChangesAsync();
        return added.Entity;
    }

    public Task<RssFeed?> GetByIdAsync(int rssFeedId) =>
        context.RssFeeds!.FindAsync(rssFeedId).AsTask();

    public Task<RssFeed?> GetByUriAsync(string uri) =>
        context.RssFeeds!.FirstOrDefaultAsync(rf => rf.Uri == uri);

    public Task<List<RssFeed>> GetByIdsAsync(IEnumerable<int> rssFeedIds)
    {
        if (rssFeedIds == null)
            return Task.FromResult(new List<RssFeed>(0));

        var idSet = rssFeedIds.ToHashSet();
        return context.RssFeeds!.Where(r => idSet.Contains(r.Id)).ToListAsync();
    }

    public Task<List<RssFeed>> FindByLastUpdatedSinceAsync(DateTime? lastUpdatedSince)
    {
        if (lastUpdatedSince.GetValueOrDefault(default) == default)
            return context.RssFeeds!.ToListAsync();
        return context.RssFeeds!.Where(r => r.LastUpdated >= lastUpdatedSince).ToListAsync();
    }

    public async Task RemoveWhereNoUserFeedAsync()
    {
        context.RssFeeds!.RemoveRange(context.RssFeeds.FromSqlRaw("select * from RssFeeds where Id not in (select RssFeedId from UserFeeds)"));
        await context.SaveChangesAsync();
    }

    public async Task<RssFeed?> UpdateDecodeBodyAsync(int rssFeedId, bool decodeBody)
    {
        var rssFeed = await GetByIdAsync(rssFeedId);
        if (rssFeed == null)
            return null;
        
        if ((rssFeed.DecodeBody ?? false) != decodeBody)
        {
            rssFeed.DecodeBody = decodeBody;
            await context.SaveChangesAsync();
        }

        return rssFeed;
    }
}