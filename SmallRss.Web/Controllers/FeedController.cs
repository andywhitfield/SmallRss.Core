using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;

namespace SmallRss.Web.Controllers
{
    [Authorize, ApiController, Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly ILogger<FeedController> _logger;
        private readonly IArticleRepository _articleRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IUserFeedRepository _userFeedRepository;
        private readonly IRssFeedRepository _rssFeedRepository;
        private readonly IUserArticlesReadRepository _userArticlesReadRepository;

        public FeedController(
            ILogger<FeedController> logger,
            IArticleRepository articleRepository,
            IUserAccountRepository userAccountRepository,
            IUserFeedRepository userFeedRepository,
            IRssFeedRepository rssFeedRepository,
            IUserArticlesReadRepository userArticlesReadRepository)
        {
            _logger = logger;
            _articleRepository = articleRepository;
            _userAccountRepository = userAccountRepository;
            _userFeedRepository = userFeedRepository;
            _rssFeedRepository = rssFeedRepository;
            _userArticlesReadRepository = userArticlesReadRepository;
        }

        [HttpGet]
        public async Task<IEnumerable<object>> Get()
        {
            _logger.LogDebug("Getting all feeds from db");
            
            var loggedInUser = await _userAccountRepository.FindOrCreateAsync(User);            
            var userFeeds = (await _userFeedRepository.GetAllByUserAsync(loggedInUser)).ToList();

            if (!userFeeds.Any())
                return new[] { new { id = "", item = "" } };

            var feeds = (await _rssFeedRepository.GetByIdsAsync(userFeeds.Select(uf => uf.RssFeedId).ToHashSet())).ToDictionary(f => f.Id);
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
            _logger.LogDebug($"Getting articles for feed {id} from db, using client UTC offset {offset}");

            var loggedInUser = await _userAccountRepository.FindOrCreateAsync(User);
            var feed = await _userFeedRepository.GetByIdAsync(id);
            if (feed == null)
                return Enumerable.Empty<Article>();

            var readArticles = await _userArticlesReadRepository.GetByUserFeedIdAsync(feed.Id);

            IEnumerable<Article> articles;
            if (loggedInUser.ShowAllItems)
                articles = await _articleRepository.GetByRssFeedIdAsync(feed.RssFeedId);
            else
                articles = await _articleRepository.GetByRssFeedIdAsync(feed.RssFeedId, readArticles);

            return articles
                .OrderBy(a => a.Published)
                .Select(a => new { read = readArticles.Any(uar => uar.ArticleId == a.Id), feed = id, story = a.Id, heading = a.Heading, article = HtmlPreview.Preview(a.Body ?? ""), posted = FriendlyDate.ToString(a.Published, offset) });
        }
    }
}
