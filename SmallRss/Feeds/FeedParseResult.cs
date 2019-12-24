namespace SmallRss.Feeds
{
    public class FeedParseResult
    {
        public static readonly FeedParseResult FailureResult = new FeedParseResult();

        public FeedParseResult()
        {
            IsValid = true;
        }

        public bool IsValid { get; }
    }
}