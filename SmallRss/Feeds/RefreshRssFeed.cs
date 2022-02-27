using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;

namespace SmallRss.Feeds
{
    public class RefreshRssFeed : IRefreshRssFeed
    {
        private readonly ILogger<RefreshRssFeed> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IFeedParser _feedParser;
        private readonly IArticleRepository _articleRepository;

        public RefreshRssFeed(ILogger<RefreshRssFeed> logger, IHttpClientFactory clientFactory, IFeedParser feedParser,
            IArticleRepository articleRepository)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _feedParser = feedParser;
            _articleRepository = articleRepository;
        }

        public async Task<bool> RefreshAsync(RssFeed rssFeed, CancellationToken cancellationToken)
        {
            using var client = _clientFactory.CreateClient(RefreshRssFeedsServiceProviderExtensions.DefaultHttpClient);
            using var response = await client.GetAsync(rssFeed.Uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Could not refresh feed {rssFeed.Id} from {rssFeed.Uri}: response status: {response.StatusCode}, content: {await response.Content.ReadAsStringAsync()}");
                return false;
            }

            _logger.LogInformation($"Successfully download feed from {rssFeed.Uri}");
            FeedParseResult parseResult;
            if (!((parseResult = await _feedParser.ParseAsync(await response.Content.ReadAsStreamAsync(), cancellationToken))?.IsValid ?? false))
            {
                _logger.LogWarning($"Could not parse feed response from {rssFeed.Uri} - content: {await response.Content.ReadAsStringAsync()}");
                return false;
            }

            _logger.LogDebug($"Feed {rssFeed.Uri} was last updated {parseResult.Feed.LastUpdated} - our version was updated: {rssFeed.LastUpdated}");
            if (!rssFeed.LastUpdated.HasValue || parseResult.Feed.LastUpdated > rssFeed.LastUpdated)
            {
                _logger.LogTrace($"Feed {rssFeed.Uri} has new items...updating articles");
                await UpdateFeedItemsAsync(rssFeed, parseResult);
                rssFeed.LastUpdated = parseResult.Feed.LastUpdated;
                rssFeed.Link = parseResult.Feed.Link;
                return true;
            }

            return false;
        }

        private async Task UpdateFeedItemsAsync(RssFeed rssFeed, FeedParseResult parseResult)
        {
            var existing = await _articleRepository.GetByRssFeedIdAsync(rssFeed.Id);
            var oldestItem = (existing?.Any() ?? false) ? existing.Min(a => a.Published ?? DateTime.MinValue) : DateTime.MinValue;

            foreach (var itemInFeed in parseResult.Articles)
            {
                var existingArticle = existing?.FirstOrDefault(e => e.ArticleGuid == itemInFeed.ArticleGuid);
                if (existingArticle != null)
                {
                    if (itemInFeed.Published > existingArticle.Published)
                    {
                        _logger.LogInformation($"Article {existingArticle.ArticleGuid} has updated ({itemInFeed.Published} - our version {existingArticle.Published}), updating our instance");
                        existingArticle.Heading = itemInFeed.Heading;
                        existingArticle.Body = itemInFeed.Body;
                        existingArticle.Url = itemInFeed.Url;
                        existingArticle.Published = itemInFeed.Published;
                        existingArticle.Author = itemInFeed.Author;
                    }
                }
                else
                {
                    _logger.LogInformation($"Add new article {itemInFeed.ArticleGuid}|{itemInFeed.Heading} to feed {rssFeed.Uri}");
                    if (itemInFeed.Published < oldestItem)
                    {
                        _logger.LogWarning($"Article {itemInFeed.ArticleGuid} is older than the oldest item...has probably already been archived so not adding.");
                        continue;
                    }

                    await _articleRepository.CreateAsync(rssFeed, itemInFeed);
                }
            }
        }
    }
}