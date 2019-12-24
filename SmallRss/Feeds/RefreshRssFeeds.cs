using System;
using System.Collections.Generic;
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
        private readonly IBackgroundServiceSettingRepository _settingsRepository;
        private readonly IRssFeedRepository _rssFeedRepository;
        private readonly IRefreshRssFeed _refreshRssFeed;

        public RefreshRssFeeds(ILogger<RefreshRssFeeds> logger, IBackgroundServiceSettingRepository settingsRepository,
            IRssFeedRepository rssFeedRepository, IRefreshRssFeed refreshRssFeed)
        {
            _logger = logger;
            _settingsRepository = settingsRepository;
            _rssFeedRepository = rssFeedRepository;
            _refreshRssFeed = refreshRssFeed;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Refreshing feeds");
            try
            {
                var refreshed = 0;
                var failed = 0;
                var feedsToRefresh = await FindFeedsToRefreshAsync();
                foreach (var rssFeed in feedsToRefresh)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;
                        
                    try
                    {
                        _logger.LogInformation($"Refreshing {rssFeed.Uri}");
                        await _refreshRssFeed.RefreshAsync(rssFeed, stoppingToken);
                        refreshed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error refreshing feed {rssFeed.Id}:{rssFeed.Uri}");
                        failed++;
                    }
                }
                _logger.LogInformation($"Completed feed refresh. {refreshed} refreshed / {failed} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing feeds");
            }
        }

        private Task<List<RssFeed>> FindFeedsToRefreshAsync()
        {
            // TOOD: work out which ones to refresh based on the time the feed was last updated and when we last checked
            // TODO: for now, just load em all
            return _rssFeedRepository.FindByLastUpdatedSinceAsync(null);
        }
    }

    public static class RefreshRssFeedsServiceProviderExtensions
    {
        public static IServiceCollection AddRefreshRssFeeds(this IServiceCollection services)
        {
            services.AddScoped<IRefreshRssFeeds, RefreshRssFeeds>();
            services.AddScoped<IRefreshRssFeed, RefreshRssFeed>();
            services.AddScoped<IRssFeedRepository, RssFeedRepository>();
            services.AddScoped<IBackgroundServiceSettingRepository, BackgroundServiceSettingRepository>();
            services.AddScoped<IFeedParser, FeedParser>();
            services.AddScoped<IFeedReader, RssFeedReader>();
            services.AddScoped<IFeedReader, AtomFeedReader>();
            services.AddHttpClient();
            return services;
        }
    }
}