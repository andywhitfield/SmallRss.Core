using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        
        //public DbSet<UserAccount> UserAccounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var sqliteConnectionString = _configuration.GetConnectionString("SmallRss");
            _logger.LogInformation($"Using Sqlite connection string: {sqliteConnectionString}");
            optionsBuilder.UseSqlite(sqliteConnectionString);
        }
    }
}