using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmallRss.Web.Controllers;

[Authorize, ApiController, Route("api/[controller]")]
public class RssController(
    ILogger<RssController> logger,
    IHttpClientFactory clientFactory)
    : ControllerBase
{
    [HttpGet]
    public async Task<object?> Get(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new { Error = "No URL specified. Please enter an RSS or Atom feed URL and try again." };

        try
        {
            var httpClient = clientFactory.CreateClient(Startup.DefaultHttpClient);
            using var response = await httpClient.GetAsync($"/api/feed/read/{HttpUtility.UrlEncode(url)}");
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode || !responseJson.TryParseJson(out ReadRssFeedResponse? readRssFeedResult, logger))
            {
                logger.LogError("Could not create feed: response code {ResponseStatusCode}: content: {ResponseJson}", response.StatusCode, responseJson);
                return null;
            }
            logger.LogTrace("Received response content:{ResponseJson}", responseJson);

            return new { readRssFeedResult?.Title };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not create feed for URL: {Url}", url);
            return new { Error = "Could not load feed, please check the URL and try again." };
        }
    }

    private class ReadRssFeedResponse
    {
        public string? Title { get; set; }
    }
}