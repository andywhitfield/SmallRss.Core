using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Feeds;

public class RssFeedImageLocator(
    ILogger<RssFeedImageLocator> logger,
    TimeProvider timeProvider,
    IHttpClientFactory httpClientFactory
) : IRssFeedImageLocator
{
    private static readonly TimeSpan RefreshPeriod = TimeSpan.FromDays(7);

    public async Task SetImageUrlAsync(RssFeed rssFeed, FeedParseResult feedParseResult, CancellationToken cancellationToken)
    {
        if (rssFeed.ImageUrlUpdated == null || timeProvider.GetUtcNow() - rssFeed.ImageUrlUpdated > RefreshPeriod)
        {
            logger.LogDebug("ImageUrlUpdated for rss feed {RssFeedId} hasn't been set, or hasn't been updated recently, will update", rssFeed.Id);
            rssFeed.ImageUrl = await GetImageUrlAsync(feedParseResult, cancellationToken);
            rssFeed.ImageUrlUpdated = timeProvider.GetUtcNow().UtcDateTime;
            
            logger.LogDebug("Updated RssFeed.{Id}: ImageUrl=[{ImageUrl}], ImageUrlUpdated=[{ImageUrlUpdated}]", rssFeed.Id, rssFeed.ImageUrl, rssFeed.ImageUrlUpdated);
        }
        else
        {
            logger.LogTrace("ImageUrlUpdated for rss feed {RssFeedId} has been recently set, nothing to do", rssFeed.Id);
        }
    }

    private async Task<string?> GetImageUrlAsync(FeedParseResult feedParseResult, CancellationToken cancellationToken)
    {
        var imageUrl = feedParseResult.Feed.ImageUrl;
        if (await IsValidUrlAsync(imageUrl, cancellationToken))
            return imageUrl;
        return null;
    }

    private async Task<bool> IsValidUrlAsync(string? url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            logger.LogDebug("Feed url [{Url}] is not valid", url);
            return false;
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient(RefreshRssFeedsServiceProviderExtensions.DefaultHttpClient);
            using var httpResponse = await httpClient.GetAsync(uri, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogDebug("Feed url {Url} did not return a success response code: {ResponseStatusCode}", uri, httpResponse.StatusCode);
                return false;
            }
    
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve resource at uri [{Uri}]", uri);
            return false;
        }
    }
}