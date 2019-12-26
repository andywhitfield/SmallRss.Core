using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public Task<List<UserArticlesRead>> GetByUserFeedIdAsync(int userFeedId)
        {
            return _context.UserArticlesRead.Where(uar => uar.UserFeedId == userFeedId).ToListAsync();
        }
    }
}