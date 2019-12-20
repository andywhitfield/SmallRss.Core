using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Data
{
    public class SqliteDataContext : DbContext
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SqliteDataContext> _logger;

        public SqliteDataContext(IConfiguration configuration, ILogger<SqliteDataContext> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        
        public DbSet<Article> Articles { get; set; }
        public DbSet<RssFeed> RssFeeds { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<UserAccountSetting> UserAccountSettings { get; set; }
        public DbSet<UserArticlesRead> UserArticlesRead { get; set; }
        public DbSet<UserFeed> UserFeeds { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var sqliteConnectionString = _configuration.GetConnectionString("SmallRss");
            _logger.LogInformation($"Using Sqlite connection string: {sqliteConnectionString}");
            optionsBuilder.UseSqlite(sqliteConnectionString);
        }
    }
}