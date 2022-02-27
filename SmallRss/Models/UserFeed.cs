using System.ComponentModel.DataAnnotations;

namespace SmallRss.Models
{
    public class UserFeed
    {
        public int Id { get; set; }
        public int UserAccountId { get; set; }
        public int RssFeedId { get; set; }

        [Required]
        public string? GroupName { get; set; }
        [Required]
        public string? Name { get; set; }
    }
}
