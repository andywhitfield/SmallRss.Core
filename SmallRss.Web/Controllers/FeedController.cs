using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;

namespace SmallRss.Web.Controllers;

[Authorize, ApiController, Route("api/[controller]")]
public class FeedController(
    ILogger<FeedController> logger,
    IArticleRepository articleRepository,
    IUserAccountRepository userAccountRepository,
    IUserFeedRepository userFeedRepository,
    IRssFeedRepository rssFeedRepository,
    IUserArticlesReadRepository userArticlesReadRepository)
    : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<object>> Get()
    {
        logger.LogDebug("Getting all feeds from db");
        
        var loggedInUser = await userAccountRepository.GetAsync(User);            
        var userFeeds = await userFeedRepository.GetAllByUserAsync(loggedInUser);

        if (!userFeeds.Any())
            return new[] { new { id = "", item = "" } };

        var feeds = (await rssFeedRepository.GetByIdsAsync(userFeeds.Select(uf => uf.RssFeedId).ToHashSet())).ToDictionary(f => f.Id);
        return userFeeds.GroupBy(f => f.GroupName).OrderBy(g => g.Key).Select(group =>
            new
            {
                id = group.Key,
                item = group.Key,
                props = new { isFolder = true, open = loggedInUser.ExpandedGroups.Contains(group.Key ?? "") },
                items = group.OrderBy(g => g.Name).Select(g =>
                    new { id = g.Id, item = g.Name, link = feeds[g.RssFeedId].Link ?? string.Empty, props = new { isFolder = false } })
            });
    }

    [HttpGet("{id}/{offset?}")]
    public async Task<IEnumerable<object>> Get(int id, int? offset)
    {
        logger.LogDebug($"Getting articles for feed {id} from db, using client UTC offset {offset}");

        var loggedInUser = await userAccountRepository.GetAsync(User);
        var feed = await userFeedRepository.GetByIdAsync(id);
        if (feed == null)
            return Enumerable.Empty<Article>();

        var readArticles = await userArticlesReadRepository.GetByUserFeedIdAsync(feed.Id);

        IEnumerable<Article> articles;
        if (loggedInUser.ShowAllItems)
            articles = await articleRepository.GetByRssFeedIdAsync(feed.RssFeedId);
        else
            articles = await articleRepository.GetByRssFeedIdAsync(feed.RssFeedId, readArticles);

        return articles
            .OrderBy(a => a.Published)
            .Select(a => new { read = readArticles.Any(uar => uar.ArticleId == a.Id), feed = id, story = a.Id, heading = a.Heading, article = HtmlPreview.Preview(a.Body ?? ""), posted = FriendlyDate.ToString(a.Published, offset) });
    }
}
