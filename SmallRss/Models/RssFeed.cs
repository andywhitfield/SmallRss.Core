using System;
using System.ComponentModel.DataAnnotations;

namespace SmallRss.Models
{
    public class RssFeed
    {
        public int Id { get; set; }
        [Required]
        public string? Uri { get; set; }
        public string? Link { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
