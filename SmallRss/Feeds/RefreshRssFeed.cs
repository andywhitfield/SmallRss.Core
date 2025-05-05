using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;

namespace SmallRss.Feeds;

public class RefreshRssFeed(ILogger<RefreshRssFeed> logger,
    IHttpClientFactory clientFactory,
    IFeedParser feedParser,
    IArticleRepository articleRepository)
    : IRefreshRssFeed
{
    public async Task<bool> RefreshAsync(RssFeed rssFeed, CancellationToken cancellationToken)
    {
        using var client = clientFactory.CreateClient(RefreshRssFeedsServiceProviderExtensions.DefaultHttpClient);
        try
        {
            using var response = await client.GetAsync(rssFeed.Uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Could not refresh feed {RssFeedId} from {RssFeedUri}: response status: {ResponseStatusCode}, content: {ResponseContent}", rssFeed.Id, rssFeed.Uri, response.StatusCode, await response.Content.ReadAsStringAsync());
                rssFeed.LastRefreshSuccess = false;
                rssFeed.LastRefreshMessage = $"Feed response: {response.StatusCode}";
                return false;
            }

            logger.LogInformation("Successfully download feed from {RssFeedUri}", rssFeed.Uri);
            FeedParseResult parseResult;
            if (!((parseResult = await feedParser.ParseAsync(await response.Content.ReadAsStreamAsync(), cancellationToken))?.IsValid ?? false))
            {
                logger.LogWarning("Could not parse feed response from {RssFeedUri} - content: {ResponseContent}", rssFeed.Uri, await response.Content.ReadAsStringAsync());
                rssFeed.LastRefreshSuccess = false;
                rssFeed.LastRefreshMessage = $"Feed response could not be parsed - invalid RSS / Atom content";
                return false;
            }

            rssFeed.LastRefreshSuccess = true;
            rssFeed.LastRefreshMessage = "";

            logger.LogDebug("Feed {RssFeedUri} was last updated {ParseResultFeedLastUpdated} - our version was updated: {RssFeedLastUpdated}", rssFeed.Uri, parseResult.Feed.LastUpdated, rssFeed.LastUpdated);
            if (!rssFeed.LastUpdated.HasValue || parseResult.Feed.LastUpdated > rssFeed.LastUpdated)
            {
                logger.LogTrace("Feed {RssFeedUri} has new items...updating articles", rssFeed.Uri);
                await UpdateFeedItemsAsync(rssFeed, parseResult);
                rssFeed.LastUpdated = parseResult.Feed.LastUpdated;
                rssFeed.Link = parseResult.Feed.Link;
                return true;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            rssFeed.LastRefreshSuccess = false;
            rssFeed.LastRefreshMessage = $"Failed to download or parse feed";
            throw;
        }

        return false;
    }

    private async Task UpdateFeedItemsAsync(RssFeed rssFeed, FeedParseResult parseResult)
    {
        var existing = await articleRepository.GetByRssFeedIdAsync(rssFeed.Id);
        var oldestItem = (existing?.Any() ?? false) ? existing.Min(a => a.Published ?? DateTime.MinValue) : DateTime.MinValue;

        foreach (var itemInFeed in parseResult.Articles)
        {
            var existingArticle = existing?.FirstOrDefault(e => IsArticleGuidMatch(e.ArticleGuid, itemInFeed.ArticleGuid));
            if (existingArticle != null)
            {
                if (itemInFeed.Published > existingArticle.Published)
                {
                    logger.LogInformation("Article {ExistingArticleArticleGuid} has updated ({ItemInFeedPublished} - our version {ExistingArticlePublished}), updating our instance", existingArticle.ArticleGuid, itemInFeed.Published, existingArticle.Published);
                    existingArticle.Heading = itemInFeed.Heading;
                    existingArticle.Body = itemInFeed.Body;
                    existingArticle.Url = itemInFeed.Url;
                    existingArticle.Published = itemInFeed.Published;
                    existingArticle.Author = itemInFeed.Author;
                }
            }
            else
            {
                logger.LogInformation("Add new article {ItemInFeedArticleGuid}|{ItemInFeedHeading} to feed {RssFeedUri}", itemInFeed.ArticleGuid, itemInFeed.Heading, rssFeed.Uri);
                if (itemInFeed.Published < oldestItem)
                {
                    logger.LogWarning("Article {ItemInFeedArticleGuid} is older than the oldest item...has probably already been archived so not adding.", itemInFeed.ArticleGuid);
                    continue;
                }

                await articleRepository.CreateAsync(rssFeed, itemInFeed);
            }
        }
    }

    private static bool IsArticleGuidMatch(string? articleGuid1, string? articleGuid2)
    {
        if (articleGuid1 == articleGuid2)
            return true;

        if (string.Equals(StripFragment(articleGuid1), StripFragment(articleGuid2), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;

        static string StripFragment(string? uri)
        {
            if (uri == null || !Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var _))
                return uri ?? "";

            uri = uri.Trim();
            var fragmentIdx = uri.LastIndexOf('#');
            if (fragmentIdx > 0)
                return uri[..fragmentIdx];
            return uri;
        }
    }
}