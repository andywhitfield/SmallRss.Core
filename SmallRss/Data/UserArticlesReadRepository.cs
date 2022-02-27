using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Data
{
    public class UserArticlesReadRepository : IUserArticlesReadRepository
    {
        private readonly ILogger<UserArticlesReadRepository> _logger;
        private readonly SqliteDataContext _context;

        public UserArticlesReadRepository(ILogger<UserArticlesReadRepository> logger, SqliteDataContext context)
        {
            _logger = logger;
            _context = context;
        }

        public Task<List<UserArticlesRead>> GetByUserFeedIdAsync(int userFeedId) =>
            _context.UserArticlesRead!.Where(uar => uar.UserFeedId == userFeedId).ToListAsync();

        public Task<IEnumerable<(int UserFeedId, string GroupName, int UnreadCount)>> FindUnreadArticlesAsync(UserAccount userAccount) =>
            _context.Database.GetDbConnection().QueryAsync<(int UserFeedId, string GroupName, int UnreadCount)>(
@"select uf.Id as UserFeedId, uf.GroupName as GroupName, COUNT(a.Id) as UnreadCount
from UserAccounts ua
join UserFeeds uf on ua.Id = uf.UserAccountId
join RssFeeds rf on rf.Id = uf.RssFeedId
left join Articles a on rf.Id = a.RssFeedId
left join UserArticlesRead uar on uar.ArticleId = a.Id and uar.UserFeedId = uf.Id and uar.UserAccountId = ua.Id
where uar.Id is null
and ua.Id = @userAccountId
group by uf.Id, uf.GroupName", new { userAccountId = userAccount.Id });

        public async Task<bool> TryCreateAsync(int userAccountId, int userFeedId, int articleId)
        {
            if (await _context.UserArticlesRead!.AnyAsync(uar =>
                uar.UserAccountId == userAccountId &&
                uar.UserFeedId == userFeedId &&
                uar.ArticleId == articleId))
                return false;
            
            await _context.UserArticlesRead!.AddAsync(new UserArticlesRead {
                UserAccountId = userAccountId,
                UserFeedId = userFeedId,
                ArticleId = articleId
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TryRemoveAsync(int userAccountId, int userFeedId, int articleId)
        {
            var userArticlesRead = await _context.UserArticlesRead!.Where(uar =>
                uar.UserAccountId == userAccountId &&
                uar.UserFeedId == userFeedId &&
                uar.ArticleId == articleId).ToListAsync();
            if (!userArticlesRead.Any())
                return false;
            
            _context.UserArticlesRead!.RemoveRange(userArticlesRead);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}