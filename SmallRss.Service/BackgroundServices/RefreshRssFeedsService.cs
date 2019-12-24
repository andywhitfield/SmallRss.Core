using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Feeds;

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
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SqliteDataContext>();
                        var feedRefreshService = scope.ServiceProvider.GetRequiredService<IRefreshRssFeeds>();
                        if (await feedRefreshService.ExecuteAsync(stoppingToken))
                            await context.SaveChangesAsync();
                    }
                }
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
    }
}