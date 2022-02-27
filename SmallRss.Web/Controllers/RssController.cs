using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SmallRss.Web.Controllers
{
    [Authorize, ApiController, Route("api/[controller]")]
    public class RssController : ControllerBase
    {
        private readonly ILogger<RssController> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public RssController(ILogger<RssController> logger,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<object?> Get(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new { Error = "No URL specified. Please enter an RSS or Atom feed URL and try again." };

            try
            {
                using var httpClient = _clientFactory.CreateClient(Startup.DefaultHttpClient);
                using var response = await httpClient.GetAsync($"/api/feed/read/{HttpUtility.UrlEncode(url)}");
                var responseJson = await response.Content.ReadAsStringAsync();
                ReadRssFeedResponse? readRssFeedResult = null;
                if (!response.IsSuccessStatusCode || !responseJson.TryParseJson(out readRssFeedResult, _logger))
                {
                    _logger.LogError($"Could not create feed: response code {response.StatusCode}: content: {responseJson}");
                    return null;
                }
                _logger.LogTrace($"Received response content:{responseJson}");

                return new { Title = readRssFeedResult?.Title };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not create feed for URL: {url}");
                return new { Error = "Could not load feed, please check the URL and try again." };
            }
        }

        private class ReadRssFeedResponse
        {
            public string? Title { get; set; }
        }
    }
}