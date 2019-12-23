using System;
using System.Collections.Generic;
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
        private readonly IBackgroundServiceSettingRepository _settingsRepository;
        private readonly IRssFeedRepository _rssFeedRepository;
        private readonly IHttpClientFactory _clientFactory;

        public RefreshRssFeeds(ILogger<RefreshRssFeeds> logger, IBackgroundServiceSettingRepository settingsRepository,
            IRssFeedRepository rssFeedRepository, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _settingsRepository = settingsRepository;
            _rssFeedRepository = rssFeedRepository;
            _clientFactory = clientFactory;
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
                    try
                    {
                        _logger.LogInformation($"Refreshing {rssFeed.Uri}");
                        await RefreshRssFeedAsync(rssFeed);
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

        private async Task RefreshRssFeedAsync(RssFeed rssFeed)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(rssFeed.Uri);
            var responseContent = await response?.Content?.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Could not refresh feed {rssFeed.Id} from {rssFeed.Uri}: response status: {response.StatusCode}, content: {responseContent}");
                return;
            }
            // TODO: parse returned content...
            _logger.LogInformation($"TODO: parse RSS or ATOM feed from response: {responseContent}");
        }
    }

    public static class RefreshRssFeedsServiceProviderExtensions
    {
        public static IServiceCollection AddRefreshRssFeeds(this IServiceCollection services)
        {
            services.AddScoped<IRefreshRssFeeds, RefreshRssFeeds>();
            services.AddScoped<IRssFeedRepository, RssFeedRepository>();
            services.AddScoped<IBackgroundServiceSettingRepository, BackgroundServiceSettingRepository>();
            services.AddHttpClient();
            return services;
        }
    }
}