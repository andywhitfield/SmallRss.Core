using SmallRss.Data;
using SmallRss.Feeds;

namespace SmallRss.Service.BackgroundServices;

public class ArticlePurging(
    IServiceProvider serviceProvider,
    ILogger<ArticlePurging> logger)
    : BackgroundService
{
    private const int PurgeCount = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Running article purge background service");
        try
        {
            await Task.Delay(3000, stoppingToken);
            do
            {
                TimeSpan timeUntilDue;
                using (var scope = serviceProvider.CreateScope())
                {
                    var backgroundServiceSettingRepository = scope.ServiceProvider.GetRequiredService<IBackgroundServiceSettingRepository>();
                    var articleRepository = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
                    (var runInterval, var lastRunDateTime) = await GetRunIntervalsAsync(backgroundServiceSettingRepository);
                    
                    var timeWhenRunDue = lastRunDateTime + runInterval;
                    logger.LogTrace("Interval: {RunInterval}; last run: {LastRunDateTime}; due: {TimeWhenRunDue}", runInterval, lastRunDateTime, timeWhenRunDue);
                    var now = DateTime.UtcNow;
                    if (timeWhenRunDue <= now)
                    {
                        logger.LogInformation("Removing old articles");
                        await articleRepository.RemoveArticlesWhereCountOverAsync(PurgeCount);
                        await articleRepository.RemoveOrphanedArticlesAsync();
                        await backgroundServiceSettingRepository.AddOrUpdateAsync("ArticlePurging.LastRunDateTime", DateParser.ToRfc3339DateTime(DateTime.UtcNow));
                        timeUntilDue = runInterval;
                    }
                    else
                    {
                        timeUntilDue = timeWhenRunDue - now;
                    }
                }

                logger.LogInformation("Article purge job - waiting [{TimeUntilDue}] before running again", timeUntilDue);
                await Task.Delay(timeUntilDue, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }
        catch (TaskCanceledException)
        {
            logger.LogDebug("Article purging background service cancellation token cancelled - service stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred running the article purge background service");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopped article purge background service");
        return Task.CompletedTask;
    }

    private async Task<(TimeSpan RunInterval, DateTime LastRunDateTime)> GetRunIntervalsAsync(IBackgroundServiceSettingRepository backgroundServiceSettingRepository)
    {
        var allSettings = await backgroundServiceSettingRepository.GetAllAsync();
        return (allSettings.FirstOrDefault(s => s.SettingName == "ArticlePurging.RunInterval")?.SettingValue?.ToTimeSpan() ?? TimeSpan.FromDays(1),
            allSettings.FirstOrDefault(s => s.SettingName == "ArticlePurging.LastRunDateTime")?.SettingValue?.ToDateTime() ?? DateTime.MinValue);
    }
}