using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IUserFeedRepository
    {
        Task<UserFeed> GetByIdAsync(int id);
        Task<List<UserFeed>> GetAllByUserAsync(UserAccount loggedInUser);
    }
}