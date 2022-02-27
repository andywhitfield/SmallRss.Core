using SmallRss.Models;

namespace SmallRss.Web.Models.Manage
{
    public class FeedSubscriptionViewModel
    {
        private readonly UserFeed _feed;
        private readonly RssFeed _rss;

        public FeedSubscriptionViewModel(UserFeed feed, RssFeed rss)
        {
            _feed = feed;
            _rss = rss;
        }

        public int Id => _feed.Id;
        public string Group => _feed.GroupName ?? "";
        public string Name => _feed.Name ?? "";
        public string Url => _rss.Uri ?? "";
    }
}