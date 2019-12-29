using System;
using System.Collections.Generic;
using SmallRss.Models;

namespace SmallRss.Feeds
{
    public class FeedParseResult
    {
        public static readonly FeedParseResult FailureResult = new FeedParseResult();

        private FeedParseResult()
        {
            IsValid = false;
        }

        public FeedParseResult(string feedTitle, RssFeed feed, IEnumerable<Article> articles)
        {
            IsValid = true;
            FeedTitle = feedTitle ?? throw new ArgumentNullException(nameof(feedTitle));
            Feed = feed ?? throw new ArgumentNullException(nameof(feed));
            Articles = articles ?? throw new ArgumentNullException(nameof(articles));
        }

        public bool IsValid { get; }
        public string FeedTitle { get; }
        public RssFeed Feed { get; }
        public IEnumerable<Article> Articles { get; }
    }
}