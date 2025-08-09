using Microsoft.EntityFrameworkCore;

namespace SmallRss.Models;

[Keyless]
public class ArticleUserFeedInfo
{
    public int ArticleId { get; set; }
    public Article? Article { get; set; }
    public string? UserFeedGroup { get; set; }
    public string? UserFeedName { get; set; }
}