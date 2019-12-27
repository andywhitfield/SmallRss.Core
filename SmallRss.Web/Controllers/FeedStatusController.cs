using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Web.Models;

namespace SmallRss.Web.Controllers
{
    [Authorize, ApiController, Route("api/[controller]")]
    public class FeedStatusController : ControllerBase
    {
        private readonly ILogger<FeedController> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IUserArticlesReadRepository _userArticlesReadRepository;

        public FeedStatusController(ILogger<FeedController> logger,
            IUserAccountRepository userAccountRepository,
            IUserArticlesReadRepository userArticlesReadRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _userArticlesReadRepository = userArticlesReadRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> Get()
        {
            var user = await _userAccountRepository.FindOrCreateAsync(User);
            return (await _userArticlesReadRepository.FindUnreadArticlesAsync(user))
                .GroupBy(f => f.GroupName)
                .Select(group =>
                    new
                    {
                        label = group.Key,
                        unread = group.Sum(g => g.UnreadCount),
                        items = group.Select(f => new { value = f.UserFeedId, unread = f.UnreadCount })
                    }
                )
                .ToList();

        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm]FeedStatusViewModel status)
        {
            _logger.LogDebug($"Updating user settings - show all: {status.ShowAll}; group: {status.Group}; expanded: {status.Expanded}");

            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            
            if (status.Expanded.HasValue && !string.IsNullOrEmpty(status.Group))
            {
                if (status.Expanded.Value) userAccount.ExpandedGroups.Add(status.Group);
                else userAccount.ExpandedGroups.Remove(status.Group);
            }

            if (status.ShowAll.HasValue)
            {
                userAccount.ShowAllItems = status.ShowAll.Value;
            }

            await _userAccountRepository.UpdateAsync(userAccount);

            return Ok();
        }
    }
}