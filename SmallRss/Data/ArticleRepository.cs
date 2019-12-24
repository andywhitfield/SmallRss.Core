using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmallRss.Models;

namespace SmallRss.Data
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly SqliteDataContext _context;

        public ArticleRepository(SqliteDataContext context)
        {
            _context = context;
        }

        public Task<List<Article>> GetByRssFeedIdAsync(int rssFeedId)
        {
            return _context.Articles.Where(a => a.RssFeedId == rssFeedId).ToListAsync();
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
    }
}