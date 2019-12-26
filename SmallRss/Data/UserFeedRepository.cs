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
    }
}