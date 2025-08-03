using Microsoft.EntityFrameworkCore;
using SmallRss.Models;

namespace SmallRss.Data;

public class UserFeedRepository(SqliteDataContext context)
    : IUserFeedRepository
{
    public Task<UserFeed?> GetByIdAsync(int id)
        => context.UserFeeds!.FindAsync(id).AsTask();

    public Task<List<UserFeed>> GetAllByUserAsync(UserAccount loggedInUser)
        => loggedInUser == null
            ? Task.FromResult(new List<UserFeed>(0))
            : context.UserFeeds!.Where(uf => uf.UserAccountId == loggedInUser.Id).ToListAsync();

    public Task<List<UserFeed>> GetAllByUserAndRssFeedAsync(UserAccount userAccount, int rssFeedId)
        => context.UserFeeds!.Where(uf => uf.UserAccountId == userAccount.Id && uf.RssFeedId == rssFeedId).ToListAsync();

    public async Task<UserFeed> CreateAsync(int rssFeedId, int userAccountId, string name, string groupName)
    {
        var userFeed = await context.UserFeeds!.AddAsync(new()
        {
            RssFeedId = rssFeedId,
            UserAccountId = userAccountId,
            Name = name,
            GroupName = groupName
        });
        await context.SaveChangesAsync();
        return userFeed.Entity;
    }

    public Task RemoveAsync(UserFeed toRemove)
    {
        context.UserArticlesRead!.RemoveRange(context.UserArticlesRead.Where(uar => uar.UserFeedId == toRemove.Id));
        context.UserFeeds!.Remove(toRemove);
        return context.SaveChangesAsync();
    }

    public Task UpdateAsync(UserFeed userFeed)
    {
        context.UserFeeds!.Update(userFeed);
        return context.SaveChangesAsync();
    }
    
    public Task<List<UserFeed>> GetAllFeedsWithUnreadArticlesAsync(UserAccount userAccount)
        => context.UserFeeds!.FromSqlInterpolated(
$@"select distinct uf.*
from Articles a
join RssFeeds rf
on rf.Id = a.RssFeedId
join UserFeeds uf
on rf.Id = uf.RssFeedId
and uf.UserAccountId = {userAccount.Id}
left join UserArticlesRead uar
on uar.ArticleId = a.Id
and uar.UserAccountId = {userAccount.Id}
where uar.Id is null").ToListAsync();
}