using System.Threading;
using System.Threading.Tasks;
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

        public FeedController(ILogger<FeedController> logger,
            IRssFeedRepository rssFeedRepository,
            IRefreshRssFeed refreshRssFeed)
        {
            _logger = logger;
            _rssFeedRepository = rssFeedRepository;
            _refreshRssFeed = refreshRssFeed;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RssFeed>> Get(int id)
        {
            var rssFeed = await _rssFeedRepository.GetByIdAsync(id);
            if (rssFeed == null)
                return NotFound();
            return rssFeed;
        }

        [HttpPost("create")]
        public async Task<ActionResult> Create([FromBody]CreateFeedModel createFeedModel)
        {
            var existing = await _rssFeedRepository.GetByUriAsync(createFeedModel.Uri);
            if (existing != null)
                return Conflict();
            var rssFeed = await _rssFeedRepository.CreateAsync(createFeedModel.Uri);
            await _refreshRssFeed.RefreshAsync(rssFeed, CancellationToken.None);
            return Created(Url.ActionLink(nameof(Get), values: new { id = rssFeed.Id }), rssFeed);
        }
   }
}
