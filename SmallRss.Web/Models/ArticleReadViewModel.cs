namespace SmallRss.Web.Models
{
    public class ArticleReadViewModel
    {
        public int? FeedId { get; set; }
        public int? StoryId { get; set; }
        public bool Read { get; set; }
        public int? MaxStoryId { get; set; }
        public int? OffsetId { get; set; }
    }
}