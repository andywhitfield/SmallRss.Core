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

        public RefreshRssFeeds(ILogger<RefreshRssFeeds> logger, IBackgroundServiceSettingRepository settingsRepository,
            IRssFeedRepository rssFeedRepository)
        {
            _logger = logger;
            _settingsRepository = settingsRepository;
            _rssFeedRepository = rssFeedRepository;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Refreshing feeds");
            try
            {
                var feedsToRefresh = await FindFeedsToRefreshAsync();
                foreach (var rssFeed in feedsToRefresh)
                {
                    _logger.LogInformation($"Refreshing {rssFeed.Uri}");
                    await RefreshRssFeedAsync(rssFeed);
                }
                _logger.LogInformation($"Completed feed refresh - updated: {feedsToRefresh.Count}");
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

        private Task RefreshRssFeedAsync(RssFeed rssFeed)
        {
            return Task.CompletedTask;
        }
    }

    public static class RefreshRssFeedsServiceProviderExtensions
    {
        public static IServiceCollection AddRefreshRssFeeds(this IServiceCollection services)
        {
            services.AddScoped<IRefreshRssFeeds, RefreshRssFeeds>();
            services.AddScoped<IRssFeedRepository, RssFeedRepository>();
            services.AddScoped<IBackgroundServiceSettingRepository, BackgroundServiceSettingRepository>();
            return services;
        }
    }
}