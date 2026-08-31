using HtmlAgilityPack;
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
        {
            logger.LogDebug("Image url [{ImageUrl}] in the rss feed is good", imageUrl);
            return imageUrl;
        }

        if (Uri.TryCreate(feedParseResult.Feed.Link, UriKind.Absolute, out var siteUri))
        {
            logger.LogDebug("Trying to get image url using a favicon on {SiteUri}", siteUri);
            imageUrl = await GetFavIconAsync(siteUri, cancellationToken);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                logger.LogDebug("Got image url favicon from {SiteUri}: {ImageUrl}", siteUri, imageUrl);
                return imageUrl;
            }
        }

        return null;
    }

    private async Task<string?> GetFavIconAsync(Uri site, CancellationToken cancellationToken)
    {
        // fallback to fav icon, first from head/link tag, then /favicon.ico
        var html = await GetPageAsync(site, cancellationToken);
        var favicon = await ExtractFaviconFromHtmlAsync(html);
        if (await IsValidUrlAsync(favicon, cancellationToken))
        {
            logger.LogDebug("Got image url from {Site} head/link tag: {ImageUrl}", site, favicon);
            return favicon;
        }

        favicon = $"{site.Scheme}://{site.DnsSafeHost}/favicon.ico";
        if (await IsValidUrlAsync(favicon, cancellationToken))
        {
            logger.LogDebug("Got image url from {Site} /favicon.ico", site);
            return favicon;
        }

        logger.LogDebug("Could not find favicon for {Site}", site);
        return null;
    }

    private async Task<string?> GetPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient(RefreshRssFeedsServiceProviderExtensions.DefaultHttpClient);
            return await httpClient.GetStringAsync(uri, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not download html from site: {Uri}", uri);
            return null;
        }
    }

    private async Task<string?> ExtractFaviconFromHtmlAsync(string? html)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                logger.LogInformation("No html returned, cannot extract favicon");
                return null;
            }

            HtmlDocument doc = new();
            doc.LoadHtml(html);
            return (
                doc.DocumentNode.SelectSingleNode("//link[@rel='shortcut icon']") ??
                doc.DocumentNode.SelectSingleNode("//link[@rel='icon']")
                )?.GetAttributeValue("href", null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not extract favicon from html");
            return null;
        }
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