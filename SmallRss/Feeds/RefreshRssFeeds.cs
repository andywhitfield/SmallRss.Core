using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;

namespace SmallRss.Feeds;

public class RefreshRssFeeds(ILogger<RefreshRssFeeds> logger,
    IRefreshRssFeed refreshRssFeed)
    : IRefreshRssFeeds
{
    public async Task ExecuteAsync(List<RssFeed> feedsToRefresh, CancellationToken cancellationToken)
    {
        if (feedsToRefresh == null)
            return;

        var updated = 0;
        logger.LogInformation("Refreshing feeds");
        try
        {
            var failed = 0;
            foreach (var rssFeed in feedsToRefresh)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    logger.LogInformation($"Refreshing {rssFeed.Uri}");
                    if (await refreshRssFeed.RefreshAsync(rssFeed, cancellationToken))
                        updated++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error refreshing feed {rssFeed.Id}:{rssFeed.Uri}");
                    failed++;
                }
            }

            logger.LogInformation($"Completed feed refresh. {updated} updated / {failed} failed / {feedsToRefresh.Count} total checked");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing feeds");
        }
    }
}

public static class RefreshRssFeedsServiceProviderExtensions
{
    public const string DefaultHttpClient = "default";

    public static IServiceCollection AddRefreshRssFeeds(this IServiceCollection services)
    {
        services.AddScoped<IRefreshRssFeeds, RefreshRssFeeds>();
        services.AddScoped<IRefreshRssFeed, RefreshRssFeed>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IRssFeedRepository, RssFeedRepository>();
        services.AddScoped<IBackgroundServiceSettingRepository, BackgroundServiceSettingRepository>();
        services.AddScoped<IFeedParser, FeedParser>();
        services.AddScoped<IFeedReader, RssFeedReader>();
        services.AddScoped<IFeedReader, AtomFeedReader>();
        services
            .AddHttpClient(DefaultHttpClient)
            .ConfigureHttpClient(c => c.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows)"))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });
        return services;
    }
}