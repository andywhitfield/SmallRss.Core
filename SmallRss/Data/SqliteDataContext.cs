using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Data;

public class SqliteDataContext(ILogger<SqliteDataContext> logger, DbContextOptions<SqliteDataContext> options)
    : DbContext(options)
{
    public DbSet<Article>? Articles { get; set; }
    public DbSet<BackgroundServiceSetting>? BackgroundServiceSettings { get; set; }
    public DbSet<RssFeed>? RssFeeds { get; set; }
    public DbSet<UserAccount>? UserAccounts { get; set; }
    public DbSet<UserAccountSetting>? UserAccountSettings { get; set; }
    public DbSet<UserArticlesRead>? UserArticlesRead { get; set; }
    public DbSet<UserFeed>? UserFeeds { get; set; }

    public void EnsureRssFeedLastRefreshColumns()
    {
        if (Database.IsSqlite())
        {
            logger.LogDebug("Checking RssFeeds is up to date");
            var columnExists = Database.GetDbConnection().ExecuteScalar<int>("SELECT COUNT(*) FROM PRAGMA_TABLE_INFO('RssFeeds') WHERE name = 'LastRefreshSuccess'");
            if (columnExists == 0)
            {
                logger.LogInformation("Creating new columns RssFeeds.LastRefreshSuccess and RssFeedsLastRefreshMessage");
                Database.ExecuteSqlRaw("ALTER TABLE RssFeeds ADD COLUMN LastRefreshSuccess BOOLEAN");
                Database.ExecuteSqlRaw("ALTER TABLE RssFeeds ADD COLUMN LastRefreshMessage TEXT");
            }
            columnExists = Database.GetDbConnection().ExecuteScalar<int>("SELECT COUNT(*) FROM PRAGMA_TABLE_INFO('RssFeeds') WHERE name = 'DecodeBody'");
            if (columnExists == 0)
            {
                logger.LogInformation("Creating new column DecodeBody");
                Database.ExecuteSqlRaw("ALTER TABLE RssFeeds ADD COLUMN DecodeBody BOOLEAN");
            }
            logger.LogDebug("RssFeeds table is up to date");
        }
    }
}