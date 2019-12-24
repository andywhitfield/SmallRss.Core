using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IArticleRepository
    {
        Task<List<Article>> GetByRssFeedIdAsync(int rssFeedId);
        Task CreateAsync(RssFeed rssFeed, Article articleToCreate);
    }
}