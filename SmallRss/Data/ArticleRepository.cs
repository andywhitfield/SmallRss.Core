using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Data
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly SqliteDataContext _context;
        private readonly ILogger<ArticleRepository> _logger;

        public ArticleRepository(SqliteDataContext context, ILogger<ArticleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<Article> GetByIdAsync(int id)
        {
            return _context.Articles.FindAsync(id).AsTask();
        }

        public Task<List<Article>> GetByRssFeedIdAsync(int rssFeedId)
        {
            return GetByRssFeedIdAsync(rssFeedId, new List<UserArticlesRead>());
        }

        public Task<List<Article>> GetByRssFeedIdAsync(int rssFeedId, List<UserArticlesRead> excludingReadArticles)
        {
            var articlesForFeed = _context.Articles.Where(a => a.RssFeedId == rssFeedId).ToListAsync();
            if (!excludingReadArticles?.Any() ?? true)
                return articlesForFeed;
            return FilterByReadAsync(articlesForFeed, excludingReadArticles);
        }

        private async Task<List<Article>> FilterByReadAsync(Task<List<Article>> articles, List<UserArticlesRead> excludingReadArticles)
        {
            return (await articles).Except(excludingReadArticles.Select(ra => new Article { Id = ra.ArticleId }), Article.IdComparer).ToList();
        }

        public Task CreateAsync(RssFeed rssFeed, Article articleToCreate)
        {
            return _context.Articles.AddAsync(new Article
            {
                Heading = articleToCreate.Heading,
                Body = articleToCreate.Body,
                Url = articleToCreate.Url,
                Published = articleToCreate.Published,
                Author = articleToCreate.Author,
                ArticleGuid = articleToCreate.ArticleGuid,
                RssFeedId = rssFeed.Id
            }).AsTask();
        }

        public Task<List<Article>> FindUnreadArticlesInUserFeedAsync(UserFeed feedToMarkAllAsRead)
        {
            return _context.Articles.FromSqlInterpolated(
$@"select a.*
from Articles a
join RssFeeds rf
on rf.Id = a.RssFeedId
left join UserArticlesRead uar
on uar.ArticleId = a.Id
and uar.UserAccountId = {feedToMarkAllAsRead.UserAccountId}
where rf.Id = {feedToMarkAllAsRead.RssFeedId}
and uar.Id is null").ToListAsync();
        }

        public async Task RemoveArticlesWhereCountOverAsync(int purgeCount)
        {
            var feedIdsWithTooManyArticles = await _context.Database.GetDbConnection().QueryAsync<int>(
                "select RssFeedId from Articles group by RssFeedId having count(1) > @purgeCount", new { purgeCount });
            foreach (var feedId in feedIdsWithTooManyArticles)
            {
                _logger.LogTrace($"Removing old articles from feed {feedId}");
                var articlesToDelete = _context.Articles.Where(a => a.RssFeedId == feedId).OrderByDescending(a => a.Published).ThenByDescending(a => a.Id).Skip(purgeCount);
                _context.Articles.RemoveRange(articlesToDelete);
                await _context.SaveChangesAsync();
            }

            _context.UserArticlesRead.RemoveRange(_context.UserArticlesRead.FromSqlRaw("select uar.* from UserArticlesRead uar left join Articles a on uar.ArticleId = a.Id where a.Id is null"));
            await _context.SaveChangesAsync();
        }
    }
}