namespace SmallRss.Models
{
    public class UserArticlesRead
    {
        public int Id { get; set; }
        public int UserAccountId { get; set; }
        public int UserFeedId { get; set; }
        public int ArticleId { get; set; }
    }
}
