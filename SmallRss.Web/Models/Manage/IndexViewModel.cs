using System.Collections.Generic;
using System.Linq;
using SmallRss.Models;

namespace SmallRss.Web.Models.Manage
{
    public class IndexViewModel
    {
        public IndexViewModel() => Feeds = new List<FeedSubscriptionViewModel>();

        public UserAccount? UserAccount { get; set; }
        public string? Error { get; set; }
        public IEnumerable<FeedSubscriptionViewModel> Feeds { get; set; }
        public IEnumerable<string> CurrentGroups => Feeds.Select(f => f.Group).Distinct().OrderBy(g => g);
    }
}