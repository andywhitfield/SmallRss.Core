using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IUserFeedRepository
    {
        Task<List<UserFeed>> GetAllByUserAsync(UserAccount loggedInUser);
    }
}