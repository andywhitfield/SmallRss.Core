using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Data;

public class SqliteDataContext(ILoggerFactory loggerFactory, IConfiguration configuration, ILogger<SqliteDataContext> logger)
    : DbContext
{
    public DbSet<Article>? Articles { get; set; }
    public DbSet<BackgroundServiceSetting>? BackgroundServiceSettings { get; set; }
    public DbSet<RssFeed>? RssFeeds { get; set; }
    public DbSet<UserAccount>? UserAccounts { get; set; }
    public DbSet<UserAccountSetting>? UserAccountSettings { get; set; }
    public DbSet<UserArticlesRead>? UserArticlesRead { get; set; }
    public DbSet<UserFeed>? UserFeeds { get; set; }

    // must be a better way to do this - ArticleUserFeedInfos isn't a real table but the result of our custom query in ArticleRepository
    // should revisit this and see if it can be converted to a regular ef query
    public DbSet<ArticleUserFeedInfo>? ArticleUserFeedInfos { get; set; }

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
            logger.LogDebug("RssFeeds table is up to date");
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLoggerFactory(loggerFactory);
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
        var sqliteConnectionString = configuration.GetConnectionString("SmallRss");
        logger.LogInformation($"Using Sqlite connection string: {sqliteConnectionString}");
        optionsBuilder.UseSqlite(sqliteConnectionString);
    }
}