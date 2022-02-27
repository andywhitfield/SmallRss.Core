using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Feeds;

namespace SmallRss.Service.BackgroundServices
{
    public class RemoveOrphanedRssFeeds : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RemoveOrphanedRssFeeds> _logger;

        public RemoveOrphanedRssFeeds(IServiceProvider serviceProvider, ILogger<RemoveOrphanedRssFeeds> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running remove orphaned rss feeds background service");
            try
            {
                await Task.Delay(2000, stoppingToken);
                do
                {
                    TimeSpan timeUntilDue;
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var backgroundServiceSettingRepository = scope.ServiceProvider.GetRequiredService<IBackgroundServiceSettingRepository>();
                        (var runInterval, var lastRunDateTime) = await GetRunIntervalsAsync(backgroundServiceSettingRepository);
                        
                        var timeWhenRunDue = lastRunDateTime + runInterval;
                        _logger.LogTrace($"Interval: {runInterval}; last run: {lastRunDateTime}; due: {timeWhenRunDue}");
                        var now = DateTime.UtcNow;
                        if (timeWhenRunDue <= now)
                        {
                            _logger.LogInformation("Removing any orphaned rss feeds");
                            await scope.ServiceProvider.GetRequiredService<IRssFeedRepository>().RemoveWhereNoUserFeedAsync();
                            await backgroundServiceSettingRepository.AddOrUpdateAsync("RemoveOrphanedRssFeeds.LastRunDateTime", DateParser.ToRfc3339DateTime(DateTime.UtcNow));
                            timeUntilDue = runInterval;
                        }
                        else
                        {
                            timeUntilDue = timeWhenRunDue - now;
                        }
                    }
 
                    _logger.LogInformation($"Remove orphaned rss feeds - waiting [{timeUntilDue}] before running again");
                    await Task.Delay(timeUntilDue, stoppingToken);
                } while (!stoppingToken.IsCancellationRequested);
            }
            catch (TaskCanceledException)
            {
                _logger.LogDebug("Remove orphaned rss feeds background service cancellation token cancelled - service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred running the remove orphaned rss feeds background service");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopped remove orphaned rss feeds background service");
            return Task.CompletedTask;
        }

        private async Task<(TimeSpan RunInterval, DateTime LastRunDateTime)> GetRunIntervalsAsync(IBackgroundServiceSettingRepository backgroundServiceSettingRepository)
        {
            var allSettings = await backgroundServiceSettingRepository.GetAllAsync();
            return (allSettings.FirstOrDefault(s => s.SettingName == "RemoveOrphanedRssFeeds.RunInterval")?.SettingValue?.ToTimeSpan() ?? TimeSpan.FromDays(1),
                allSettings.FirstOrDefault(s => s.SettingName == "RemoveOrphanedRssFeeds.LastRunDateTime")?.SettingValue?.ToDateTime() ?? DateTime.MinValue);
        }
    }
}