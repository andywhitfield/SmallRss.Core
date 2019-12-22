using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IUserFeedRepository
    {
        Task<IEnumerable<UserFeed>> GetAllByUserAsync(UserAccount loggedInUser);
    }
}