using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallRss.Data;
using SmallRss.Models;
using SmallRss.Web.Models;

namespace SmallRss.Web.Controllers;

[Authorize, ApiController, Route("api/[controller]")]
public class ArticleController(ILogger<FeedController> logger,
    IArticleRepository articleRepository,
    IUserAccountRepository userAccountRepository,
    IUserFeedRepository userFeedRepository,
    IUserArticlesReadRepository userArticlesReadRepository)
    : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> Get(int id)
    {
        var article = await articleRepository.GetByIdAsync(id);
        if (article == null)
            return NotFound();
        return new
        {
            Id = id,
            Body = HttpUtility.HtmlDecode(article.Body ?? string.Empty),
            Url = article.Url ?? string.Empty,
            Author = article.Author ?? string.Empty
        };
    }

    [HttpPost]
    public async Task<IEnumerable<object>> PostAsync([FromForm] ArticleReadViewModel feed)
    {
        logger.LogDebug("Marking story [{StoryId}] as {ReadOrUnread} for feed {FeedId}", feed.StoryId, feed.Read ? "read" : "unread", feed.FeedId);

        List<Article> newArticles = [];

        var userAccount = await userAccountRepository.GetAsync(User);
        var userFeedId = 0;
        if (feed.FeedId.HasValue && !feed.StoryId.HasValue)
        {
            if (!feed.MaxStoryId.HasValue || feed.MaxStoryId.Value <= 0)
                feed.MaxStoryId = int.MaxValue;

            logger.LogDebug("Marking all stories as {ReadOrUnread}: {FeedId} up to id {MaxStoryId}", feed.Read ? "read" : "unread", feed.FeedId, feed.MaxStoryId);
            userFeedId = feed.FeedId.Value;

            if (userFeedId == -1)
            {
                var userFeedsWithUnreadArticles = await userFeedRepository.GetAllFeedsWithUnreadArticlesAsync(userAccount);
                foreach (var userFeedWithUnreadArticles in userFeedsWithUnreadArticles)
                    await MarkAllAsReadAsync(userFeedWithUnreadArticles.Id);
            }
            else
            {
                await MarkAllAsReadAsync(feed.FeedId.Value);
            }

            async Task MarkAllAsReadAsync(int userFeedId)
            {
                var feedToMarkAllAsRead = await userFeedRepository.GetByIdAsync(userFeedId);
                if (feedToMarkAllAsRead != null && feedToMarkAllAsRead.UserAccountId == userAccount.Id)
                {
                    foreach (var article in await articleRepository.FindUnreadArticlesInUserFeedAsync(feedToMarkAllAsRead))
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
        }
        else if (feed.StoryId.HasValue)
        {
            var article = await articleRepository.GetByIdAsync(feed.StoryId.Value);
            var feedToMarkAsRead = (await userFeedRepository.GetAllByUserAndRssFeedAsync(userAccount, article?.RssFeedId ?? 0)).FirstOrDefault();
            if (article != null && feedToMarkAsRead != null)
            {
                userFeedId = feedToMarkAsRead.Id;
                await MarkAsAsync(feedToMarkAsRead, article.Id, feed.Read);
            }
            else
            {
                logger.LogWarning("Feed {FeedId} could not be found or is not associated with the current user, will not make any changes", feed.FeedId);
            }
        }

        return newArticles
            .OrderBy(a => a.Published)
            .Select(a => new { read = false, feed = userFeedId, story = a.Id, heading = a.Heading, article = HtmlPreview.Preview(a.Body ?? ""), posted = FriendlyDate.ToString(a.Published, feed.OffsetId) });
    }

    private Task<bool> MarkAsAsync(UserFeed feed, int articleId, bool read)
    {
        logger.LogDebug("Marking article {ArticleId} in feed {FeedId} as {ReadOrUnread}", articleId, feed.Id, read ? "read" : "unread");
        if (read)
            return userArticlesReadRepository.TryCreateAsync(feed.UserAccountId, feed.Id, articleId);
        return userArticlesReadRepository.TryRemoveAsync(feed.UserAccountId, feed.Id, articleId);
    }
}
