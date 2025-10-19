using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        if (userFeeds.Count == 0)
            return [new { id = "", item = "" }];

        var feeds = (await rssFeedRepository.GetByIdsAsync(userFeeds.Select(uf => uf.RssFeedId).ToHashSet())).ToDictionary(f => f.Id);
        return userFeeds.GroupBy(f => f.GroupName).OrderBy(g => g.Key)
            .Select(group => (group.Key, Items: group.Select(g => (g.Id, g.Name, g.RssFeedId))))
            .Append((Key: "All unread", Items: [(Id: -1, Name: "All unread", RssFeedId: -1)]))
            .Select(group =>
                new
                {
                    id = group.Key,
                    item = group.Key,
                    props = new { isFolder = true, open = loggedInUser.ExpandedGroups.Contains(group.Key ?? "") },
                    items = group.Items.OrderBy(g => g.Name).Select(g =>
                        new { id = g.Id, item = g.Name, link = feeds.GetValueOrDefault(g.RssFeedId)?.Link ?? string.Empty, props = new { isFolder = false } })
                });
    }

    [HttpGet("{id}")]
    public async IAsyncEnumerable<object> Get(int id, [FromQuery] int? offset, [FromQuery] int? toggle = 0)
    {
        logger.LogDebug("Getting articles for feed {Id} from db, using client UTC offset {Offset}, toggle sort order {Toggle}", id, offset, toggle);

        var loggedInUser = await userAccountRepository.GetAsync(User);

        IEnumerable<Article> articles;
        List<UserArticlesRead> readArticles;
        ILookup<int, ArticleUserFeedInfo>? articleUserFeedInfoForAllUnread = null;
        if (id == -1)
        {
            if (toggle == 1)
            {
                loggedInUser.AllUnreadSortOrder = loggedInUser.AllUnreadSortOrder == "GroupAscThenDateAsc" ? "" : "GroupAscThenDateAsc";
                await userAccountRepository.UpdateAsync(loggedInUser);
                logger.LogDebug("Saved new sort order for user [{UserAccountId}]=[{AllUnreadSortOrder}]", loggedInUser.Id, loggedInUser.AllUnreadSortOrder);
            }

            readArticles = [];
            articleUserFeedInfoForAllUnread = await articleRepository.GetAllUnreadArticlesAsync(loggedInUser).ToLookupAsync(x => x.ArticleId);
            articles = articleUserFeedInfoForAllUnread.Select(a => a.First().Article!);

            if (loggedInUser.AllUnreadSortOrder == "GroupAscThenDateAsc")
            {
                articles = articles
                    .OrderBy(a => articleUserFeedInfoForAllUnread[a.Id].FirstOrDefault()?.UserFeedGroup ?? "")
                    .ThenBy(a => articleUserFeedInfoForAllUnread[a.Id].FirstOrDefault()?.UserFeedName ?? "")
                    .ThenBy(a => a.Published);
            }
            else
            {
                articles = articles.OrderBy(a => a.Published);
            }
        }
        else
        {
            var feed = await userFeedRepository.GetByIdAsync(id);
            if (feed == null)
                yield break;

            readArticles = await userArticlesReadRepository.GetByUserFeedIdAsync(feed.Id);

            if (loggedInUser.ShowAllItems)
                articles = await articleRepository.GetByRssFeedIdAsync(feed.RssFeedId);
            else
                articles = await articleRepository.GetByRssFeedIdAsync(feed.RssFeedId, readArticles);

            articles = articles.OrderBy(a => a.Published);
        }

        foreach (var article in articles)
            yield return new { read = readArticles.Any(uar => uar.ArticleId == article.Id), feed = article.RssFeedId, feedInfo = id == -1 ? GetFeedInfo(articleUserFeedInfoForAllUnread, article) : null, story = article.Id, heading = article.Heading, article = HtmlPreview.Preview(article.Body ?? ""), posted = FriendlyDate.ToString(article.Published, offset) };
    }

    private static object GetFeedInfo(ILookup<int, ArticleUserFeedInfo>? articleUserFeedInfoForAllUnread, Article article)
    {
        var aufi = articleUserFeedInfoForAllUnread?[article.Id].FirstOrDefault();
        return new { group = aufi?.UserFeedGroup ?? "", name = aufi?.UserFeedName ?? "" };
    }
}
