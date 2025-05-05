using SmallRss.Data;
using SmallRss.Feeds;

namespace SmallRss.Service.BackgroundServices;

public class RefreshRssFeedsService(
    IServiceProvider serviceProvider,
    ILogger<RefreshRssFeedsService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Running refresh rss feeds background service");
        try
        {
            await Task.Delay(1000, stoppingToken);
            do
            {
                TimeSpan fastRefreshInterval;
                DateTime? loadFeedsUpdatedSince = null;
                using (var scope = serviceProvider.CreateScope())
                {
                    var backgroundServiceSettingRepository = scope.ServiceProvider.GetRequiredService<IBackgroundServiceSettingRepository>();
                    TimeSpan slowRefreshInterval;
                    DateTime lastSlowRefreshDateTime;
                    (fastRefreshInterval, slowRefreshInterval, lastSlowRefreshDateTime) = await GetFeedRefreshIntervalsAsync(backgroundServiceSettingRepository);
                    
                    var timeWhenFullRefreshDue = lastSlowRefreshDateTime + slowRefreshInterval;
                    logger.LogTrace($"Fast refresh interval: {fastRefreshInterval}; slow refresh interval: {slowRefreshInterval}; " +
                        $"last refresh: {lastSlowRefreshDateTime}; time until next full refresh: {timeWhenFullRefreshDue}");
                        
                    if (timeWhenFullRefreshDue <= DateTime.UtcNow)
                    {
                        logger.LogInformation($"Refreshing all feeds");
                        await backgroundServiceSettingRepository.AddOrUpdateAsync("LastSlowRefreshDateTime", DateParser.ToRfc3339DateTime(DateTime.UtcNow));
                    }
                    else
                    {
                        var secondsTillFullRefresh = (timeWhenFullRefreshDue - DateTime.UtcNow).TotalSeconds;
                        var diff = (int)((secondsTillFullRefresh / slowRefreshInterval.TotalSeconds) * 100);
                        var loadFeedsUpdatedPeriod = TimeSpan.FromHours(2);
                        if (diff % 30 == 0)
                            loadFeedsUpdatedPeriod = TimeSpan.FromDays(3);
                        else if (diff % 15 == 0)
                            loadFeedsUpdatedPeriod = TimeSpan.FromDays(1);
                        else if (diff % 10 == 0)
                            loadFeedsUpdatedPeriod = TimeSpan.FromHours(12);
                        else if (diff % 5 == 0)
                            loadFeedsUpdatedPeriod = TimeSpan.FromHours(6);
                        logger.LogTrace($"Refresh feed period: {loadFeedsUpdatedPeriod} based on time till next refresh diff: {diff}%");
                        loadFeedsUpdatedSince = DateTime.UtcNow - loadFeedsUpdatedPeriod;
                    }
                }

                using (var scope = serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<SqliteDataContext>();
                    var rssFeedRepository = scope.ServiceProvider.GetRequiredService<IRssFeedRepository>();
                    var feedRefreshService = scope.ServiceProvider.GetRequiredService<IRefreshRssFeeds>();

                    logger.LogInformation($"Getting feeds updated after: {loadFeedsUpdatedSince}");
                    try
                    {
                        var feedsToRefresh = await rssFeedRepository.FindByLastUpdatedSinceAsync(loadFeedsUpdatedSince);
                        await feedRefreshService.ExecuteAsync(feedsToRefresh, stoppingToken);
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred refreshing rss feeds");
                    }
                }

                logger.LogInformation($"Refreshed rss feeds - waiting [{fastRefreshInterval}] before running again");
                await Task.Delay(fastRefreshInterval, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }
        catch (TaskCanceledException)
        {
            logger.LogDebug("Rss feed background service cancellation token cancelled - service stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred running the refresh rss feeds background service");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopped refresh rss feeds background service");
        return Task.CompletedTask;
    }

    private async Task<(TimeSpan FastRefreshInterval, TimeSpan SlowRefreshInterval, DateTime LastSlowRefreshDateTime)> GetFeedRefreshIntervalsAsync(IBackgroundServiceSettingRepository backgroundServiceSettingRepository)
    {
        var allSettings = await backgroundServiceSettingRepository.GetAllAsync();
        return (allSettings.FirstOrDefault(s => s.SettingName == "FastRefreshInterval")?.SettingValue?.ToTimeSpan() ?? TimeSpan.FromMinutes(10),
            allSettings.FirstOrDefault(s => s.SettingName == "SlowRefreshInterval")?.SettingValue?.ToTimeSpan() ?? TimeSpan.FromDays(1),
            allSettings.FirstOrDefault(s => s.SettingName == "LastSlowRefreshDateTime")?.SettingValue?.ToDateTime() ?? DateTime.MinValue);
    }
}