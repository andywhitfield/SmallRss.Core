using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;
using SmallRss.Web.Models;

namespace SmallRss.Web.Controllers
{
    [Authorize, ApiController, Route("api/[controller]")]
    public class ArticleController : ControllerBase
    {
        private readonly ILogger<FeedController> _logger;
        private readonly IArticleRepository _articleRepository;

        public ArticleController(ILogger<FeedController> logger, IArticleRepository articleRepository)
        {
            _logger = logger;
            _articleRepository = articleRepository;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Article>> Get(int id)
        {
            var article = await _articleRepository.GetByIdAsync(id);
            if (article == null)
                return NotFound();
            return new Article { Id = id, Body = HttpUtility.HtmlDecode(article.Body), Url = article.Url, Author = article.Author };
        }

        [HttpPost]
        public ActionResult<IEnumerable<object>> Post([FromForm]ArticleReadViewModel feed)
        {
            _logger.LogDebug($"Marking story as {(feed.Read ? "read" : "unread")}: {feed.Story}");
            var newArticles = new List<Article>();
            var userFeedId = 0;
            /*
            TODO
            var user = this.CurrentUser(datastore);
            var userFeedId = 0;
            if (feed.Feed.HasValue && !feed.Story.HasValue)
            {
                if (!feed.MaxStory.HasValue || feed.MaxStory.Value <= 0)
                    feed.MaxStory = int.MaxValue;

                _logger.LogDebug($"Marking all stories as {(feed.Read ? "read" : "unread")}: {feed.Feed} up to id {feed.MaxStory}");
                userFeedId = feed.Feed.Value;

                var feedToMarkAllAsRead = datastore.Load<UserFeed>(feed.Feed.Value);
                if (feedToMarkAllAsRead != null && feedToMarkAllAsRead.UserAccountId == user.Id)
                {
                    foreach (var article in datastore.LoadUnreadArticlesInUserFeed(feedToMarkAllAsRead).ToList())
                    {
                        if (article.Id > feed.MaxStory)
                        {
                            newArticles.Add(article);
                            continue;
                        }
                        MarkAsRead(feedToMarkAllAsRead, article.Id, feed.Read);
                    }
                }
            }
            else if (feed.Story.HasValue)
            {
                var article = datastore.Load<Article>(feed.Story.Value);
                var feedToMarkAsRead = datastore.LoadAll<UserFeed>(Tuple.Create<string, object, ClauseComparsion>("RssFeedId", article.RssFeedId, ClauseComparsion.Equals), Tuple.Create<string, object, ClauseComparsion>("UserAccountId", user.Id, ClauseComparsion.Equals)).FirstOrDefault();
                if (feedToMarkAsRead != null && feedToMarkAsRead.UserAccountId == user.Id)
                {
                    userFeedId = feedToMarkAsRead.Id;
                    MarkAsRead(feedToMarkAsRead, article.Id, feed.Read);
                }
                else
                {
                    _logger.LogWarning($"Feed {feed.Feed} could not be found or is not associated with the current user, will not make any changes");
                }
            }
            */

            return newArticles
                .OrderBy(a => a.Published)
                .Select(a => new { read = false, feed = userFeedId, story = a.Id, heading = a.Heading, article = HtmlPreview.Preview(a.Body), posted = FriendlyDate.ToString(a.Published, feed.Offset) })
                .ToList();
        }

        private void MarkAsRead(UserFeed feed, int articleId, bool read)
        {
            /*
            var userArticleRead = new UserArticlesRead { UserAccountId = feed.UserAccountId, UserFeedId = feed.Id, ArticleId = articleId };
            if (read)
            {
                if (!datastore.LoadAll<UserArticlesRead>(
                    Tuple.Create("UserAccountId", (object)userArticleRead.UserAccountId, ClauseComparsion.Equals),
                    Tuple.Create("UserFeedId", (object)userArticleRead.UserFeedId, ClauseComparsion.Equals),
                    Tuple.Create("ArticleId", (object)userArticleRead.ArticleId, ClauseComparsion.Equals)
                    ).Any())
                {
                    datastore.Store(userArticleRead);
                }
            }
            else
            {
                datastore.RemoveUserArticleRead(userArticleRead);
            }
            */
        }
    }
}
