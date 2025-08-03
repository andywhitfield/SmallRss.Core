using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Data;

public class ArticleRepository(SqliteDataContext context, ILogger<ArticleRepository> logger)
    : IArticleRepository
{
    public Task<Article?> GetByIdAsync(int id) =>
        context.Articles!.FindAsync(id).AsTask();

    public Task<List<Article>> GetByRssFeedIdAsync(int rssFeedId) =>
        GetByRssFeedIdAsync(rssFeedId, []);

    public Task<List<Article>> GetByRssFeedIdAsync(int rssFeedId, List<UserArticlesRead>? excludingReadArticles)
    {
        var articlesForFeed = context.Articles!.Where(a => a.RssFeedId == rssFeedId).ToListAsync();
        if ((excludingReadArticles?.Count ?? 0) == 0)
            return articlesForFeed;
        return FilterByReadAsync(articlesForFeed, excludingReadArticles);
    }

    private async Task<List<Article>> FilterByReadAsync(Task<List<Article>> articles, List<UserArticlesRead>? excludingReadArticles) =>
        (await articles).Except((excludingReadArticles ?? Enumerable.Empty<UserArticlesRead>()).Select(ra => new Article { Id = ra.ArticleId }), Article.IdComparer).ToList();

    public Task CreateAsync(RssFeed rssFeed, Article articleToCreate) =>
        context.Articles!.AddAsync(new Article
        {
            Heading = articleToCreate.Heading,
            Body = articleToCreate.Body,
            Url = articleToCreate.Url,
            Published = articleToCreate.Published,
            Author = articleToCreate.Author,
            ArticleGuid = articleToCreate.ArticleGuid,
            RssFeedId = rssFeed.Id
        }).AsTask();

    public Task<List<Article>> FindUnreadArticlesInUserFeedAsync(UserFeed feedToMarkAllAsRead) =>
        context.Articles!.FromSqlInterpolated(
$@"select a.*
from Articles a
join RssFeeds rf
on rf.Id = a.RssFeedId
left join UserArticlesRead uar
on uar.ArticleId = a.Id
and uar.UserAccountId = {feedToMarkAllAsRead.UserAccountId}
where rf.Id = {feedToMarkAllAsRead.RssFeedId}
and uar.Id is null").ToListAsync();

    public async Task RemoveArticlesWhereCountOverAsync(int purgeCount)
    {
        var feedIdsWithTooManyArticles = await context.Database.GetDbConnection().QueryAsync<int>(
            "select RssFeedId from Articles group by RssFeedId having count(1) > @purgeCount", new { purgeCount });
        foreach (var feedId in feedIdsWithTooManyArticles)
        {
            logger.LogTrace("Removing old articles from feed {FeedId}", feedId);
            var articlesToDelete = context.Articles!.Where(a => a.RssFeedId == feedId).OrderByDescending(a => a.Published).ThenByDescending(a => a.Id).Skip(purgeCount);
            context.Articles!.RemoveRange(articlesToDelete);
            await context.SaveChangesAsync();
        }

        context.UserArticlesRead!.RemoveRange(context.UserArticlesRead.FromSqlRaw("select uar.* from UserArticlesRead uar left join Articles a on uar.ArticleId = a.Id where a.Id is null"));
        await context.SaveChangesAsync();
    }

    public Task<List<Article>> GetAllUnreadArticlesAsync(UserAccount userAccount)
        => context.Articles!.FromSqlInterpolated(
$@"select a.*
from Articles a
join RssFeeds rf
on rf.Id = a.RssFeedId
join UserFeeds uf
on rf.Id = uf.RssFeedId
and uf.UserAccountId = {userAccount.Id}
left join UserArticlesRead uar
on uar.ArticleId = a.Id
and uar.UserAccountId = {userAccount.Id}
where uar.Id is null").ToListAsync();
}