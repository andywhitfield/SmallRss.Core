using Microsoft.EntityFrameworkCore;
using Serilog;
using SmallRss.Data;
using SmallRss.Feeds;
using SmallRss.Service.BackgroundServices;

namespace SmallRss.Service;

public class Startup
{
    public Startup(IWebHostEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
        Configuration = builder.Build();
    }

    public IConfigurationRoot Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(Configuration);

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });

        services.AddDbContext<SqliteDataContext>((sp, options) =>
        {
#if DEBUG
            options.EnableSensitiveDataLogging();
#endif
            var sqliteConnectionString = Configuration.GetConnectionString("SmallRss");
            sp.GetRequiredService<ILogger<Startup>>().LogInformation("Using Sqlite connection string: {SqliteConnectionString}", sqliteConnectionString);
            options.UseSqlite(sqliteConnectionString);            
        });

        services.AddMvc();
        services.AddCors();
        services.AddDistributedMemoryCache();

        services.AddRefreshRssFeeds();
        services.AddHostedService<RefreshRssFeedsService>();
        services.AddHostedService<RemoveOrphanedRssFeeds>();
        services.AddHostedService<ArticlePurging>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.UseEndpoints(options => options.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"));

        using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.Database.EnsureCreated();
        // should move to EF migrations, but for now, just create the RssFeed columns if required
        context.EnsureRssFeedLastRefreshColumns();
    }
}
