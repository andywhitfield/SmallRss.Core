using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Web.Models;

namespace SmallRss.Web.Controllers;

[Authorize, ApiController, Route("api/[controller]")]
public class FeedStatusController(ILogger<FeedController> logger,
    IUserAccountRepository userAccountRepository,
    IUserArticlesReadRepository userArticlesReadRepository)
    : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<object>> Get()
    {
        var user = await userAccountRepository.GetAsync(User);
        return (await userArticlesReadRepository.FindUnreadArticlesAsync(user))
            .GroupBy(f => f.GroupName)
            .Select(group =>
                new
                {
                    label = group.Key,
                    unread = group.Sum(g => g.UnreadCount),
                    items = group.Select(f => new { value = f.UserFeedId, unread = f.UnreadCount })
                }
            );
    }

    [HttpPost]
    public async Task<ActionResult> Post([FromForm]FeedStatusViewModel status)
    {
        logger.LogDebug($"Updating user settings - show all: {status.ShowAll}; group: {status.Group}; expanded: {status.Expanded}");

        var userAccount = await userAccountRepository.GetAsync(User);
        
        if (status.Expanded.HasValue && !string.IsNullOrEmpty(status.Group))
        {
            if (status.Expanded.Value) userAccount.ExpandedGroups.Add(status.Group);
            else userAccount.ExpandedGroups.Remove(status.Group);
        }

        if (status.ShowAll.HasValue)
        {
            userAccount.ShowAllItems = status.ShowAll.Value;
        }

        await userAccountRepository.UpdateAsync(userAccount);

        return Ok();
    }
}