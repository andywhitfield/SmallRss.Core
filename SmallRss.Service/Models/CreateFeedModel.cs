using System.ComponentModel.DataAnnotations;

namespace SmallRss.Service.Models
{
    public class CreateFeedModel
    {
        [Required, MinLength(4)]
        public string? Uri { get; set; }
        [Required]
        public int UserAccountId { get; set; }
    }
}