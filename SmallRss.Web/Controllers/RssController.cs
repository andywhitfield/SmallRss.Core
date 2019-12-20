using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SmallRss.Web.Controllers
{
    [Authorize, ApiController, Route("api/[controller]")]
    public class RssController : ControllerBase
    {
        private readonly ILogger<RssController> _logger;

        public RssController(ILogger<RssController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<object> Get(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new { Error = "No URL specified. Please enter an RSS or Atom feed URL and try again." };

            try
            {
                //var feed = feedFactory.CreateFeed(new Uri(url));
                //return new { Title = feed.Title };
                // TODO
                throw new NotImplementedException("TODO");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not create feed for URL: {url}");
                return new { Error = "Could not load feed, please check the URL and try again." };
            }
        }
    }
}