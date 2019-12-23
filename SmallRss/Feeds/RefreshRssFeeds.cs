using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmallRss.Feeds
{
    public class RefreshRssFeeds : IRefreshRssFeeds
    {
        private readonly ILogger<RefreshRssFeeds> _logger;

        public RefreshRssFeeds(ILogger<RefreshRssFeeds> logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Let's refresh some feeds");
            return Task.CompletedTask;
        }
    }
}