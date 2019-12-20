namespace SmallRss.Models
{
    public class UserFeed
    {
        public int Id { get; set; }
        public int UserAccountId { get; set; }
        public int RssFeedId { get; set; }

        public string GroupName { get; set; }
        public string Name { get; set; }
    }
}
