using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SmallRss.Web.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly ILogger<FeedController> _logger;

        public FeedController(ILogger<FeedController> logger)
        {
            _logger = logger;
        }

        [HttpPut("refresh/{userAccountId}/{feedId?}")]
        public ActionResult Refresh(int userAccountId, int? feedId)
        {
            _logger.LogDebug($"Refresh feed {(feedId.HasValue ? feedId.ToString() : "<all>")} for user {userAccountId}");
            return Accepted();
        }
   }
}
