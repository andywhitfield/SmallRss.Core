using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Feeds
{
    public class RefreshRssFeed : IRefreshRssFeed
    {
        private readonly ILogger<RefreshRssFeed> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IFeedParser _feedParser;

        public RefreshRssFeed(ILogger<RefreshRssFeed> logger, IHttpClientFactory clientFactory, IFeedParser feedParser)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _feedParser = feedParser;
        }

        public async Task RefreshAsync(RssFeed rssFeed, CancellationToken cancellationToken)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(rssFeed.Uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Could not refresh feed {rssFeed.Id} from {rssFeed.Uri}: response status: {response.StatusCode}, content: {await response.Content?.ReadAsStringAsync()}");
                return;
            }
            _logger.LogInformation($"Successfully download feed from {rssFeed.Uri}");
            if (!((await _feedParser.ParseAsync(await response.Content.ReadAsStreamAsync(), cancellationToken))?.IsValid ?? false))
            {
                _logger.LogWarning($"Could not parse feed response from {rssFeed.Uri} - content: {await response.Content.ReadAsStringAsync()}");
                return;
            }
            // TODO: update our db with the updated article(s) and feed details
        }
    }
}