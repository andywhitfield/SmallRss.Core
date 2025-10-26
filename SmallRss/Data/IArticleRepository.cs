using SmallRss.Models;

namespace SmallRss.Data;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(int id);
    Task<List<Article>> GetByRssFeedIdAsync(int rssFeedId);
    Task<List<Article>> GetByRssFeedIdAsync(int rssFeedId, List<UserArticlesRead> excludingReadArticles);
    Task CreateAsync(RssFeed rssFeed, Article articleToCreate);
    Task<List<Article>> FindUnreadArticlesInUserFeedAsync(UserFeed feedToMarkAllAsRead);
    Task RemoveArticlesWhereCountOverAsync(int purgeCount);
    Task RemoveOrphanedArticlesAsync();
    IAsyncEnumerable<ArticleUserFeedInfo> GetAllUnreadArticlesAsync(UserAccount userAccount);
}