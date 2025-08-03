using SmallRss.Models;

namespace SmallRss.Data;

public interface IUserFeedRepository
{
    Task<UserFeed?> GetByIdAsync(int id);
    Task<List<UserFeed>> GetAllByUserAsync(UserAccount loggedInUser);
    Task<List<UserFeed>> GetAllByUserAndRssFeedAsync(UserAccount userAccount, int rssFeedId);
    Task<UserFeed> CreateAsync(int rssFeedId, int userAccountId, string name, string groupName);
    Task RemoveAsync(UserFeed toRemove);
    Task UpdateAsync(UserFeed userFeed);
    Task<List<UserFeed>> GetAllFeedsWithUnreadArticlesAsync(UserAccount userAccount);
}