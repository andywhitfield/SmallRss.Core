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
    public class ArticlePurging : BackgroundService
    {
        private const int PurgeCount = 200;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ArticlePurging> _logger;

        public ArticlePurging(IServiceProvider serviceProvider, ILogger<ArticlePurging> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running article purge background service");
            try
            {
                await Task.Delay(3000, stoppingToken);
                do
                {
                    TimeSpan timeUntilDue;
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var backgroundServiceSettingRepository = scope.ServiceProvider.GetRequiredService<IBackgroundServiceSettingRepository>();
                        var articleRepository = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
                        (var runInterval, var lastRunDateTime) = await GetRunIntervalsAsync(backgroundServiceSettingRepository);
                        
                        var timeWhenRunDue = lastRunDateTime + runInterval;
                        _logger.LogTrace($"Interval: {runInterval}; last run: {lastRunDateTime}; due: {timeWhenRunDue}");
                        var now = DateTime.UtcNow;
                        if (timeWhenRunDue <= now)
                        {
                            _logger.LogInformation("Removing old articles");
                            await articleRepository.RemoveArticlesWhereCountOverAsync(PurgeCount);
                            await backgroundServiceSettingRepository.AddOrUpdateAsync("ArticlePurging.LastRunDateTime", DateParser.ToRfc3339DateTime(DateTime.UtcNow));
                            timeUntilDue = runInterval;
                        }
                        else
                        {
                            timeUntilDue = timeWhenRunDue - now;
                        }
                    }
 
                    _logger.LogInformation($"Article purge job - waiting [{timeUntilDue}] before running again");
                    await Task.Delay(timeUntilDue, stoppingToken);
                } while (!stoppingToken.IsCancellationRequested);
            }
            catch (TaskCanceledException)
            {
                _logger.LogDebug("Article purging background service cancellation token cancelled - service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred running the article purge background service");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopped article purge background service");
            return Task.CompletedTask;
        }

        private async Task<(TimeSpan RunInterval, DateTime LastRunDateTime)> GetRunIntervalsAsync(IBackgroundServiceSettingRepository backgroundServiceSettingRepository)
        {
            var allSettings = await backgroundServiceSettingRepository.GetAllAsync();
            return (allSettings.FirstOrDefault(s => s.SettingName == "ArticlePurging.RunInterval")?.SettingValue?.ToTimeSpan() ?? TimeSpan.FromDays(1),
                allSettings.FirstOrDefault(s => s.SettingName == "ArticlePurging.LastRunDateTime")?.SettingValue?.ToDateTime() ?? DateTime.MinValue);
        }
    }
}