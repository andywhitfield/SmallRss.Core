using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Feeds;
using SmallRss.Models;

namespace SmallRss.Service.BackgroundServices
{
    public class RefreshRssFeedsService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RefreshRssFeedsService> _logger;

        public RefreshRssFeedsService(IServiceProvider serviceProvider, ILogger<RefreshRssFeedsService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running refresh rss feeds background service");
            try
            {
                do
                {
                    TimeSpan fastRefreshInterval;
                    DateTime? loadFeedsUpdatedSince = null;
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var backgroundServiceSettingRepository = scope.ServiceProvider.GetRequiredService<IBackgroundServiceSettingRepository>();
                        TimeSpan slowRefreshInterval;
                        DateTime lastSlowRefreshDateTime;
                        (fastRefreshInterval, slowRefreshInterval, lastSlowRefreshDateTime) = await GetFeedRefreshIntervalsAsync(backgroundServiceSettingRepository);
                        
                        var timeWhenFullRefreshDue = lastSlowRefreshDateTime + slowRefreshInterval;
                        if (timeWhenFullRefreshDue < DateTime.UtcNow)
                        {
                            _logger.LogInformation($"Refreshing all feeds");
                            await backgroundServiceSettingRepository.AddOrUpdateAsync("LastSlowRefreshDateTime", DateParser.ToRfc3339DateTime(DateTime.UtcNow));
                        }
                        else
                        {
                            var diff = (int)((slowRefreshInterval.TotalSeconds - fastRefreshInterval.TotalSeconds) / slowRefreshInterval.TotalSeconds) * 100;
                            var loadFeedsUpdatedPeriod = TimeSpan.FromHours(2);
                            if (diff % 30 == 0)
                                loadFeedsUpdatedPeriod = TimeSpan.FromDays(3);
                            else if (diff % 15 == 0)
                                loadFeedsUpdatedPeriod = TimeSpan.FromDays(1);
                            else if (diff % 10 == 0)
                                loadFeedsUpdatedPeriod = TimeSpan.FromHours(12);
                            else if (diff % 5 == 0)
                                loadFeedsUpdatedPeriod = TimeSpan.FromHours(6);
                            _logger.LogTrace($"Refresh feed period: {loadFeedsUpdatedPeriod}");
                            loadFeedsUpdatedSince = DateTime.UtcNow - loadFeedsUpdatedPeriod;
                        }
                    }

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SqliteDataContext>();
                        var rssFeedRepository = scope.ServiceProvider.GetRequiredService<IRssFeedRepository>();
                        var feedRefreshService = scope.ServiceProvider.GetRequiredService<IRefreshRssFeeds>();

                        _logger.LogInformation($"Getting feeds updated after: {loadFeedsUpdatedSince}");
                        var feedsToRefresh = await rssFeedRepository.FindByLastUpdatedSinceAsync(loadFeedsUpdatedSince);
                        if (await feedRefreshService.ExecuteAsync(feedsToRefresh, stoppingToken))
                            await context.SaveChangesAsync();
                    }

                    _logger.LogInformation($"Refreshed rss feeds - waiting [{fastRefreshInterval}] before running again");
                    await Task.Delay(fastRefreshInterval, stoppingToken);
                } while (!stoppingToken.IsCancellationRequested);
            }
            catch (TaskCanceledException)
            {
                _logger.LogDebug("Rss feed background service cancellation token cancelled - service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred running the refresh rss feeds background service");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopped refresh rss feeds background service");
            return Task.CompletedTask;
        }

        private async Task<(TimeSpan FastRefreshInterval, TimeSpan SlowRefreshInterval, DateTime LastSlowRefreshDateTime)> GetFeedRefreshIntervalsAsync(IBackgroundServiceSettingRepository backgroundServiceSettingRepository)
        {
            var allSettings = await backgroundServiceSettingRepository.GetAllAsync();
            return (allSettings.FirstOrDefault(s => s.SettingName == "FastRefreshInterval")?.SettingValue.ToTimeSpan() ?? TimeSpan.FromMinutes(10),
                allSettings.FirstOrDefault(s => s.SettingName == "SlowRefreshInterval")?.SettingValue.ToTimeSpan() ?? TimeSpan.FromDays(1),
                allSettings.FirstOrDefault(s => s.SettingName == "LastSlowRefreshDateTime")?.SettingValue.ToDateTime() ?? DateTime.MinValue);
        }
    }
}