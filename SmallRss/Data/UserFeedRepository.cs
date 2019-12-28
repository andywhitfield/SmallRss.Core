using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmallRss.Models;

namespace SmallRss.Data
{
    public class UserFeedRepository : IUserFeedRepository
    {
        private readonly SqliteDataContext _context;

        public UserFeedRepository(SqliteDataContext context)
        {
            _context = context;
        }

        public Task<UserFeed> GetByIdAsync(int id)
        {
            return _context.UserFeeds.FindAsync(id).AsTask();
        }

        public Task<List<UserFeed>> GetAllByUserAsync(UserAccount loggedInUser)
        {
            if (loggedInUser == null)
                return Task.FromResult(new List<UserFeed>(0));

            return _context.UserFeeds.Where(uf => uf.UserAccountId == loggedInUser.Id).ToListAsync();
        }

        public Task<List<UserFeed>> GetAllByUserAndRssFeedAsync(UserAccount userAccount, int rssFeedId)
        {
            return _context.UserFeeds.Where(uf => uf.UserAccountId == userAccount.Id && uf.RssFeedId == rssFeedId).ToListAsync();
        }

        public async Task<UserFeed> CreateAsync(int rssFeedId, int userAccountId, string name, string groupName)
        {
            var userFeed = await _context.UserFeeds.AddAsync(new UserFeed {
                RssFeedId = rssFeedId,
                UserAccountId = userAccountId,
                Name = name,
                GroupName = groupName
            });
            await _context.SaveChangesAsync();
            return userFeed.Entity;
        }

        public Task RemoveAsync(UserFeed toRemove)
        {
            _context.UserArticlesRead.RemoveRange(_context.UserArticlesRead.Where(uar => uar.UserFeedId == toRemove.Id));
            _context.UserFeeds.Remove(toRemove);
            return _context.SaveChangesAsync();
        }

        public Task UpdateAsync(UserFeed userFeed)
        {
            _context.UserFeeds.Update(userFeed);
            return _context.SaveChangesAsync();
        }
    }
}