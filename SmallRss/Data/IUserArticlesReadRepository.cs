using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IUserArticlesReadRepository
    {
        Task<List<UserArticlesRead>> GetByUserFeedIdAsync(int userFeedId);
    }
}