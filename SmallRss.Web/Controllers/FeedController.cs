using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Web.Controllers
{
    [Authorize, ApiController, Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly ILogger<FeedController> _logger;

        public FeedController(ILogger<FeedController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<object>> Get()
        {
            _logger.LogDebug("Getting all feeds from db");
            
            //var loggedInUser = this.CurrentUser(datastore);
            var userFeeds = new UserFeed[0]; // TODO: datastore.LoadAll<UserFeed>("UserAccountId", loggedInUser.Id);

            if (!userFeeds.Any())
                return new[] { new { id = "", item = "" } };

            return null;
            /*
            return userFeeds.GroupBy(f => f.GroupName).OrderBy(g => g.Key).Select(group =>
                new
                {
                    id = group.Key,
                    item = group.Key,
                    props = new { isFolder = true, open = loggedInUser.ExpandedGroups.Contains(group.Key) },
                    items = group.OrderBy(g => g.Name).Select(g =>
                        new { id = g.Id, item = g.Name, link = datastore.Load<RssFeed>(g.RssFeedId).Link ?? string.Empty, props = new { isFolder = false } })
                });
            */
        }

        [HttpGet("{id}/{offset}")]
        public ActionResult<IEnumerable<object>> Get(int id, int? offset)
        {
            _logger.LogDebug("Getting articles for feed {0} from db, using client UTC offset {1}", id, offset);

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
