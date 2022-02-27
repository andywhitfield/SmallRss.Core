using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmallRss.Models
{
    public class Article
    {
        public static readonly IEqualityComparer<Article> IdComparer = new IdEqualityComparer();
        
        public int Id { get; set; }
        public int RssFeedId { get; set; }
        [Required]
        public string? ArticleGuid { get; set; }
        public string? Heading { get; set; }
        public string? Body { get; set; }
        public string? Url { get; set; }
        public string? Author { get; set; }
        public DateTime? Published { get; set; }

        private class IdEqualityComparer : IEqualityComparer<Article>
        {
            public bool Equals(Article? x, Article? y)
            {
                return x?.Id == y?.Id;
            }

            public int GetHashCode(Article? obj)
            {
                return obj?.Id ?? 0;
            }
        }
    }
}
