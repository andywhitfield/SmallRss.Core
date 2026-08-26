using Microsoft.EntityFrameworkCore;
using SmallRss.Models;

namespace SmallRss.Data;

public class SqliteDataContext(DbContextOptions<SqliteDataContext> options)
    : DbContext(options), ISqliteDataContext
{
    public DbSet<Article>? Articles { get; set; }
    public DbSet<BackgroundServiceSetting>? BackgroundServiceSettings { get; set; }
    public DbSet<RssFeed>? RssFeeds { get; set; }
    public DbSet<UserAccount>? UserAccounts { get; set; }
    public DbSet<UserAccountSetting>? UserAccountSettings { get; set; }
    public DbSet<UserArticlesRead>? UserArticlesRead { get; set; }
    public DbSet<UserFeed>? UserFeeds { get; set; }

    public void Migrate() => Database.Migrate();
}