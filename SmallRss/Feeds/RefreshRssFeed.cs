using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;

namespace SmallRss.Feeds;

public class RefreshRssFeed(ILogger<RefreshRssFeed> logger, IHttpClientFactory clientFactory, IFeedParser feedParser,
    IArticleRepository articleRepository)
    : IRefreshRssFeed
{
    public async Task<bool> RefreshAsync(RssFeed rssFeed, CancellationToken cancellationToken)
    {
        using var client = clientFactory.CreateClient(RefreshRssFeedsServiceProviderExtensions.DefaultHttpClient);
        using var response = await client.GetAsync(rssFeed.Uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning($"Could not refresh feed {rssFeed.Id} from {rssFeed.Uri}: response status: {response.StatusCode}, content: {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        logger.LogInformation($"Successfully download feed from {rssFeed.Uri}");
        FeedParseResult parseResult;
        if (!((parseResult = await feedParser.ParseAsync(await response.Content.ReadAsStreamAsync(), cancellationToken))?.IsValid ?? false))
        {
            logger.LogWarning($"Could not parse feed response from {rssFeed.Uri} - content: {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        logger.LogDebug($"Feed {rssFeed.Uri} was last updated {parseResult.Feed.LastUpdated} - our version was updated: {rssFeed.LastUpdated}");
        if (!rssFeed.LastUpdated.HasValue || parseResult.Feed.LastUpdated > rssFeed.LastUpdated)
        {
            logger.LogTrace($"Feed {rssFeed.Uri} has new items...updating articles");
            await UpdateFeedItemsAsync(rssFeed, parseResult);
            rssFeed.LastUpdated = parseResult.Feed.LastUpdated;
            rssFeed.Link = parseResult.Feed.Link;
            return true;
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
                    logger.LogInformation($"Article {existingArticle.ArticleGuid} has updated ({itemInFeed.Published} - our version {existingArticle.Published}), updating our instance");
                    existingArticle.Heading = itemInFeed.Heading;
                    existingArticle.Body = itemInFeed.Body;
                    existingArticle.Url = itemInFeed.Url;
                    existingArticle.Published = itemInFeed.Published;
                    existingArticle.Author = itemInFeed.Author;
                }
            }
            else
            {
                logger.LogInformation($"Add new article {itemInFeed.ArticleGuid}|{itemInFeed.Heading} to feed {rssFeed.Uri}");
                if (itemInFeed.Published < oldestItem)
                {
                    logger.LogWarning($"Article {itemInFeed.ArticleGuid} is older than the oldest item...has probably already been archived so not adding.");
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