﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Web.Models;

namespace SmallRss.Web.Controllers
{
    [Authorize, ApiController, Route("api/[controller]")]
    public class FeedStatusController : ControllerBase
    {
        private readonly ILogger<FeedController> _logger;

        public FeedStatusController(ILogger<FeedController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<object>> Get()
        {
            /*
            var user = this.CurrentUser(datastore);
            return datastore
                .GetUnreadFeeds(user.Id)
                .GroupBy(f => f.GroupName)
                .Select(group =>
                    new
                    {
                        label = group.Key,
                        unread = group.Sum(g => g.UnreadCount),
                        items = group.Select(f => new { value = f.FeedId, unread = f.UnreadCount })
                    }
                );
            */
            return new object[0];
        }

        [HttpPost]
        public ActionResult Post(FeedStatusViewModel status)
        {
            _logger.LogDebug($"Updating user settings - show all: {status.ShowAll}; group: {status.Group}; expanded: {status.Expanded}");

            /*
            var user = this.CurrentUser(datastore);
            if (status.Expanded.HasValue && !string.IsNullOrEmpty(status.Group))
            {
                if (status.Expanded.Value) user.ExpandedGroups.Add(status.Group);
                else user.ExpandedGroups.Remove(status.Group);
            }
            if (status.ShowAll.HasValue)
            {
                user.ShowAllItems = status.ShowAll.Value;
            }
            datastore.UpdateAccount(user);
            */
            return Ok();
        }
    }
}