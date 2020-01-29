using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;

namespace SmallRss.Feeds
{
    public class RefreshRssFeeds : IRefreshRssFeeds
    {
        private readonly ILogger<RefreshRssFeeds> _logger;
        private readonly IRssFeedRepository _rssFeedRepository;
        private readonly IRefreshRssFeed _refreshRssFeed;

        public RefreshRssFeeds(ILogger<RefreshRssFeeds> logger, IRssFeedRepository rssFeedRepository, IRefreshRssFeed refreshRssFeed)
        {
            _logger = logger;
            _rssFeedRepository = rssFeedRepository;
            _refreshRssFeed = refreshRssFeed;
        }

        public async Task<bool> ExecuteAsync(List<RssFeed> feedsToRefresh, CancellationToken cancellationToken)
        {
            if (feedsToRefresh == null)
                return false;

            var updated = 0;
            _logger.LogInformation("Refreshing feeds");
            try
            {
                var failed = 0;
                foreach (var rssFeed in feedsToRefresh)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        _logger.LogInformation($"Refreshing {rssFeed.Uri}");
                        if (await _refreshRssFeed.RefreshAsync(rssFeed, cancellationToken))
                            updated++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error refreshing feed {rssFeed.Id}:{rssFeed.Uri}");
                        failed++;
                    }
                }

                _logger.LogInformation($"Completed feed refresh. {updated} updated / {failed} failed / {feedsToRefresh.Count} total checked");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing feeds");
            }

            return updated > 0;
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
}