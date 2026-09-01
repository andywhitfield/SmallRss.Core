using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using SmallRss.Data;

namespace SmallRss.Web.Controllers;

[Authorize, Route("[controller]")]
public class FeedIconController(
    ILogger<FeedIconController> logger,
    IUserAccountRepository userAccountRepository,
    IUserFeedRepository userFeedRepository,
    IRssFeedRepository rssFeedRepository,
    IHttpClientFactory httpClientFactory)
    : Controller
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var loggedInUser = await userAccountRepository.GetAsync(User);
        var rssFeed = await rssFeedRepository.GetByIdAsync(id);
        if (rssFeed == null)
        {
            logger.LogWarning("Rss feed {Id} does not exist", id);
            return NotFound();
        }

        logger.LogDebug("Checking user has access to rss feed {Id}", id);
        var userFeeds = await userFeedRepository.GetAllByUserAndRssFeedAsync(loggedInUser, rssFeed.Id);
        if (userFeeds.Count == 0)
        {
            logger.LogWarning("Rss feed {RssFeedId} is not available to user {UserAccountId}", id, loggedInUser.Id);
            return NotFound();
        }

        if (!Uri.TryCreate(rssFeed.ImageUrl, UriKind.Absolute, out var imageUrl))
        {
            logger.LogWarning("Image url for rss feed {Id} is not a valid url, returning an empty image", id);
            return Redirect("/images/missing.png");
        }

        logger.LogDebug("Getting image url: {ImageUrl}", imageUrl);
        try
        {
            var httpClient = httpClientFactory.CreateClient(Startup.FeedIconHttpClient);
            using var httpResponse = await httpClient.GetAsync(imageUrl);
            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Image url for rss feed {Id} did not return a success response {ResponseCode}, returning an empty image", id, httpResponse.StatusCode);
                return Redirect("/images/missing.png");
            }

            Response.StatusCode = StatusCodes.Status200OK;
            Response.Headers.ContentLength = GetContentLength(httpResponse);
            Response.Headers.ContentType = GetHeaderValues(httpResponse, "Content-Type");
            Response.Headers.LastModified = GetHeaderValues(httpResponse, "Last-Modified");
            Response.Headers.ETag = httpResponse.Headers.ETag?.Tag;
            await httpResponse.Content.CopyToAsync(Response.Body);

            return Empty;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get image url for rss feed {Id} [{Url}], returning an empty image", id, imageUrl);
            return Redirect("/images/missing.png");
        }
    }

    private static long? GetContentLength(HttpResponseMessage httpResponse)
        => httpResponse.Headers.TryGetValues("Content-Length", out var headers) && headers.Any() && long.TryParse(headers.FirstOrDefault(), out var contentLength)
            ? contentLength
            : null;

    private static StringValues GetHeaderValues(HttpResponseMessage httpResponse, string name)
        => new(httpResponse.Headers.TryGetValues(name, out var contentType) ? contentType.ToArray() : []);
}
