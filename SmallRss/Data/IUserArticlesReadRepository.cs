using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IUserArticlesReadRepository
    {
        Task<List<UserArticlesRead>> GetByUserFeedIdAsync(int userFeedId);
        Task<IEnumerable<(int UserFeedId, string GroupName, int UnreadCount)>> FindUnreadArticlesAsync(UserAccount userAccount);
        Task<bool> TryCreateAsync(int userAccountId, int userFeedId, int articleId);
        Task<bool> TryRemoveAsync(int userAccountId, int userFeedId, int articleId);
    }
}