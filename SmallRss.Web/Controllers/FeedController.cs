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
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IUserFeedRepository _userFeedRepository;
        private readonly IRssFeedRepository _rssFeedRepository;

        public FeedController(
            ILogger<FeedController> logger,
            IUserAccountRepository userAccountRepository,
            IUserFeedRepository userFeedRepository,
            IRssFeedRepository rssFeedRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _userFeedRepository = userFeedRepository;
            _rssFeedRepository = rssFeedRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> Get()
        {
            _logger.LogDebug("Getting all feeds from db");
            
            var loggedInUser = await _userAccountRepository.FindByUserPrincipalAsync(User);            
            var userFeeds = (await _userFeedRepository.GetAllByUserAsync(loggedInUser)).ToList();

            if (!userFeeds.Any())
                return new[] { new { id = "", item = "" } };

            var feeds = (await _rssFeedRepository.GetByIdsAsync(userFeeds.Select(uf => uf.RssFeedId).ToHashSet())).ToDictionary(f => f.Id);
            return userFeeds.GroupBy(f => f.GroupName).OrderBy(g => g.Key).Select(group =>
                new
                {
                    id = group.Key,
                    item = group.Key,
                    props = new { isFolder = true, open = loggedInUser.ExpandedGroups.Contains(group.Key) },
                    items = group.OrderBy(g => g.Name).Select(g =>
                        new { id = g.Id, item = g.Name, link = feeds[g.RssFeedId].Link ?? string.Empty, props = new { isFolder = false } })
                })
                .ToList();
        }

        [HttpGet("{id}/{offset}")]
        public ActionResult<IEnumerable<object>> Get(int id, int? offset)
        {
            _logger.LogDebug($"Getting articles for feed {id} from db, using client UTC offset {offset}");

            /*
            var loggedInUser = this.CurrentUser(datastore);
            var feed = datastore.Load<UserFeed>(id);
            var readArticles = datastore.LoadAll<UserArticlesRead>("UserFeedId", feed.Id).ToList();

            IEnumerable<Article> articles;
            if (loggedInUser.ShowAllItems)
            {
                articles = datastore.LoadAll<Article>("RssFeedId", feed.RssFeedId);
            }
            else
            {
                articles = datastore.LoadUnreadArticlesInUserFeed(feed);
            }

            return articles
                .OrderBy(a => a.Published)
                .Select(a => new { read = readArticles.Any(uar => uar.ArticleId == a.Id), feed = id, story = a.Id, heading = a.Heading, article = HtmlPreview.Preview(a.Body), posted = FriendlyDate.ToString(a.Published, offset) });
            */

            return new object[0];
        }
    }
}
