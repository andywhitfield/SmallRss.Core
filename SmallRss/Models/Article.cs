using System;
using System.ComponentModel.DataAnnotations;

namespace SmallRss.Models
{
    public class Article
    {
        public int Id { get; set; }
        public int RssFeedId { get; set; }
        [Required]
        public string ArticleGuid { get; set; }
        public string Heading { get; set; }
        public string Body { get; set; }
        public string Url { get; set; }
        public string Author { get; set; }
        public DateTime? Published { get; set; }
    }
}
