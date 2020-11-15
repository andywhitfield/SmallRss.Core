using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SmallRss.Data;
using SmallRss.Feeds;
using SmallRss.Service.BackgroundServices;

namespace SmallRss.Service
{
    public class Startup
    {
        private IWebHostEnvironment hostingEnvironment;

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            hostingEnvironment = env;
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

            services.AddDbContext<SqliteDataContext>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
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
            scope.ServiceProvider.GetRequiredService<SqliteDataContext>().Database.EnsureCreated();
        }
    }
}
