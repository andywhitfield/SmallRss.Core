using System.ComponentModel.DataAnnotations.Schema;

namespace SmallRss.Models;

public class ArticleUserFeedInfo
{
    public int ArticleId { get; set; }
    [NotMapped]
    public Article? Article { get; set; }
    public string? UserFeedGroup { get; set; }
    public string? UserFeedName { get; set; }
}