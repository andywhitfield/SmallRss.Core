using System.ComponentModel.DataAnnotations;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using SmallRss.Data;
using SmallRss.Feeds;
using SmallRss.Models;
using SmallRss.Service.Models;

namespace SmallRss.Service.Controllers;

[ApiController, Route("api/[controller]")]
public class FeedController(ILogger<FeedController> logger,
    IRssFeedRepository rssFeedRepository,
    IRefreshRssFeed refreshRssFeed,
    IFeedParser feedParser,
    IHttpClientFactory clientFactory,
    SqliteDataContext context)
    : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<RssFeed>> Get(int id)
    {
        var rssFeed = await rssFeedRepository.GetByIdAsync(id);
        if (rssFeed == null)
            return NotFound();
        return rssFeed;
    }

    [HttpGet("read/{uri}")]
    public async Task<ActionResult<object>> Read([Required] string uri)
    {
        if (!Uri.TryCreate(HttpUtility.UrlDecode(uri), UriKind.Absolute, out var feedUri))
        {
            logger.LogWarning($"Could not parse uri {uri}");
            return BadRequest();
        }

        using var client = clientFactory.CreateClient(RefreshRssFeedsServiceProviderExtensions.DefaultHttpClient);
        using var response = await client.GetAsync(feedUri.ToString(), CancellationToken.None);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning($"Could not load feed {feedUri}: response status: {response.StatusCode}, content: {await response.Content.ReadAsStringAsync()}");
            return BadRequest();
        }

        logger.LogInformation($"Successfully download feed from {feedUri}");
        FeedParseResult parseResult;
        if (!((parseResult = await feedParser.ParseAsync(await response.Content.ReadAsStreamAsync(), CancellationToken.None))?.IsValid ?? false))
        {
            logger.LogWarning($"Could not parse feed response from {feedUri} - content: {await response.Content.ReadAsStringAsync()}");
            return BadRequest();
        }

        return new { Title = parseResult.FeedTitle, parseResult.Feed.LastUpdated, parseResult.Feed.Link };
    }

    [HttpPost("create")]
    public async Task<ActionResult> Create([FromBody] CreateFeedModel createFeedModel)
    {
        var existing = await rssFeedRepository.GetByUriAsync(createFeedModel.Uri ?? "");
        if (existing != null)
            return Conflict();

        var rssFeed = await rssFeedRepository.CreateAsync(createFeedModel.Uri ?? "");
        if (await refreshRssFeed.RefreshAsync(rssFeed, CancellationToken.None))
            await context.SaveChangesAsync();

        return Created(Url.ActionLink(nameof(Get), values: new { id = rssFeed.Id }) ?? "", rssFeed);
    }

    [HttpPost("refresh/{id}")]
    public async Task<ActionResult> Refresh([Required, FromRoute] int id)
    {
        var rssFeed = await rssFeedRepository.GetByIdAsync(id);
        if (rssFeed == null)
            return NotFound();

        if (await refreshRssFeed.RefreshAsync(rssFeed, CancellationToken.None))
            return Ok();
        return NoContent();
    }
}
