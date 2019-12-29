using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Feeds;
using SmallRss.Models;
using SmallRss.Service.Models;

namespace SmallRss.Web.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly ILogger<FeedController> _logger;
        private readonly IRssFeedRepository _rssFeedRepository;
        private readonly IRefreshRssFeed _refreshRssFeed;
        private readonly IFeedParser _feedParser;
        private readonly IHttpClientFactory _clientFactory;
        private readonly SqliteDataContext _context;

        public FeedController(ILogger<FeedController> logger,
            IRssFeedRepository rssFeedRepository,
            IRefreshRssFeed refreshRssFeed,
            IFeedParser feedParser,
            IHttpClientFactory clientFactory,
            SqliteDataContext context)
        {
            _logger = logger;
            _rssFeedRepository = rssFeedRepository;
            _refreshRssFeed = refreshRssFeed;
            _feedParser = feedParser;
            _clientFactory = clientFactory;
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RssFeed>> Get(int id)
        {
            var rssFeed = await _rssFeedRepository.GetByIdAsync(id);
            if (rssFeed == null)
                return NotFound();
            return rssFeed;
        }

        [HttpGet("read/{uri}")]
        public async Task<ActionResult<object>> Read([Required]string uri)
        {
            if (!Uri.TryCreate(HttpUtility.UrlDecode(uri), UriKind.Absolute, out var feedUri))
            {
                _logger.LogWarning($"Could not parse uri {uri}");
                return BadRequest();
            }
            
            using var client = _clientFactory.CreateClient(RefreshRssFeedsServiceProviderExtensions.DefaultHttpClient);
            using var response = await client.GetAsync(feedUri.ToString(), CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Could not load feed {feedUri}: response status: {response.StatusCode}, content: {await response.Content?.ReadAsStringAsync()}");
                return BadRequest();
            }

            _logger.LogInformation($"Successfully download feed from {feedUri}");
            FeedParseResult parseResult;
            if (!((parseResult = await _feedParser.ParseAsync(await response.Content.ReadAsStreamAsync(), CancellationToken.None))?.IsValid ?? false))
            {
                _logger.LogWarning($"Could not parse feed response from {feedUri} - content: {await response.Content.ReadAsStringAsync()}");
                return BadRequest();
            }

            return new { Title = parseResult.FeedTitle, parseResult.Feed.LastUpdated, parseResult.Feed.Link };
        }

        [HttpPost("create")]
        public async Task<ActionResult> Create([FromBody]CreateFeedModel createFeedModel)
        {
            var existing = await _rssFeedRepository.GetByUriAsync(createFeedModel.Uri);
            if (existing != null)
                return Conflict();
            
            var rssFeed = await _rssFeedRepository.CreateAsync(createFeedModel.Uri);
            if (await _refreshRssFeed.RefreshAsync(rssFeed, CancellationToken.None))
                await _context.SaveChangesAsync();
            
            return Created(Url.ActionLink(nameof(Get), values: new { id = rssFeed.Id }), rssFeed);
        }
   }
}
