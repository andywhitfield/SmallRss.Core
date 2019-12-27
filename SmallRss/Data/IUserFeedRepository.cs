using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IUserFeedRepository
    {
        Task<UserFeed> GetByIdAsync(int id);
        Task<List<UserFeed>> GetAllByUserAsync(UserAccount loggedInUser);
        Task<List<UserFeed>> GetAllByUserAndRssFeedAsync(UserAccount userAccount, int rssFeedId);
    }
}