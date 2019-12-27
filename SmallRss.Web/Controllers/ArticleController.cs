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
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IUserFeedRepository _userFeedRepository;
        private readonly IUserArticlesReadRepository _userArticlesReadRepository;

        public ArticleController(ILogger<FeedController> logger,
            IArticleRepository articleRepository,
            IUserAccountRepository userAccountRepository,
            IUserFeedRepository userFeedRepository,
            IUserArticlesReadRepository userArticlesReadRepository)
        {
            _logger = logger;
            _articleRepository = articleRepository;
            _userAccountRepository = userAccountRepository;
            _userFeedRepository = userFeedRepository;
            _userArticlesReadRepository = userArticlesReadRepository;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Article>> Get(int id)
        {
            var article = await _articleRepository.GetByIdAsync(id);
            if (article == null)
                return NotFound();
            return new Article { Id = id, Body = HttpUtility.HtmlDecode(article.Body ?? string.Empty), Url = article.Url ?? string.Empty, Author = article.Author ?? string.Empty };
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<object>>> PostAsync([FromForm]ArticleReadViewModel feed)
        {
            _logger.LogDebug($"Marking story [{feed.StoryId}] as {(feed.Read ? "read" : "unread")} for feed {feed.FeedId}");
            
            var newArticles = new List<Article>();

            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            var userFeedId = 0;
            if (feed.FeedId.HasValue && !feed.StoryId.HasValue)
            {
                if (!feed.MaxStoryId.HasValue || feed.MaxStoryId.Value <= 0)
                    feed.MaxStoryId = int.MaxValue;

                _logger.LogDebug($"Marking all stories as {(feed.Read ? "read" : "unread")}: {feed.FeedId} up to id {feed.MaxStoryId}");
                userFeedId = feed.FeedId.Value;

                var feedToMarkAllAsRead = await _userFeedRepository.GetByIdAsync(feed.FeedId.Value);
                if (feedToMarkAllAsRead != null && feedToMarkAllAsRead.UserAccountId == userAccount.Id)
                {
                    foreach (var article in await _articleRepository.FindUnreadArticlesInUserFeedAsync(feedToMarkAllAsRead))
                    {
                        if (article.Id > feed.MaxStoryId)
                        {
                            newArticles.Add(article);
                            continue;
                        }
                        await MarkAsAsync(feedToMarkAllAsRead, article.Id, feed.Read);
                    }
                }
            }
            else if (feed.StoryId.HasValue)
            {
                var article = await _articleRepository.GetByIdAsync(feed.StoryId.Value);
                var feedToMarkAsRead = (await _userFeedRepository.GetAllByUserAndRssFeedAsync(userAccount, article.RssFeedId)).FirstOrDefault();
                if (feedToMarkAsRead != null)
                {
                    userFeedId = feedToMarkAsRead.Id;
                    await MarkAsAsync(feedToMarkAsRead, article.Id, feed.Read);
                }
                else
                {
                    _logger.LogWarning($"Feed {feed.FeedId} could not be found or is not associated with the current user, will not make any changes");
                }
            }

            return newArticles
                .OrderBy(a => a.Published)
                .Select(a => new { read = false, feed = userFeedId, story = a.Id, heading = a.Heading, article = HtmlPreview.Preview(a.Body), posted = FriendlyDate.ToString(a.Published, feed.OffsetId) })
                .ToList();
        }

        private Task MarkAsAsync(UserFeed feed, int articleId, bool read)
        {
            if (read)
                return _userArticlesReadRepository.TryCreateAsync(feed.UserAccountId, feed.Id, articleId);
            return _userArticlesReadRepository.TryRemoveAsync(feed.UserAccountId, feed.Id, articleId);
        }
    }
}
