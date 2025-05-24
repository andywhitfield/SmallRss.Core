using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using SmallRss.Data;
using SmallRss.Web.Authorisation;

namespace SmallRss.Web;

public class Startup
{
    public const string DefaultHttpClient = "default";
    public const string RaindropHttpClient = "raindrop";

    private readonly IWebHostEnvironment hostingEnvironment;

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

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        var tokenLifespan = TimeSpan.FromDays(7);
        services.AddSingleton(Configuration);

        services
            .AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(o =>
            {
                o.LoginPath = "/signin";
                o.LogoutPath = "/signout";
                o.Cookie.HttpOnly = true;
                o.Cookie.MaxAge = tokenLifespan;
                o.ExpireTimeSpan = tokenLifespan;
                o.SlidingExpiration = true;
            });

        services
            .AddDataProtection()
            .SetApplicationName(typeof(Startup).Namespace ?? "")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(hostingEnvironment.ContentRootPath, ".keys")));

        services
            .AddSession(options => options.IdleTimeout = TimeSpan.FromMinutes(5))
            .AddFido2(options =>
            {
                options.ServerName = "Small:Rss";
                options.ServerDomain = Configuration.GetValue<string>("FidoDomain");
                options.Origins = [Configuration.GetValue<string>("FidoOrigins")];
            });
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });

        services.Configure<CookiePolicyOptions>(o =>
        {
            o.CheckConsentNeeded = context => false;
            o.MinimumSameSitePolicy = SameSiteMode.None;
        });

        services.AddDbContext<SqliteDataContext>();

        services.AddMvc().AddSessionStateTempDataProvider();
        services.AddRazorPages();
        services.AddCors();
        services.AddDistributedMemoryCache();
        services.AddSession(options => options.IdleTimeout = TimeSpan.FromMinutes(5));
        
        services.AddScoped<IRssFeedRepository, RssFeedRepository>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IUserFeedRepository, UserFeedRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IUserArticlesReadRepository, UserArticlesReadRepository>();
        services.AddScoped<IAuthorisationHandler, AuthorisationHandler>();
        services.AddHttpClient(DefaultHttpClient).ConfigureHttpClient(c => c.BaseAddress = new Uri(Configuration.GetValue<string>("ServiceUri") ?? throw new Exception("ServiceUri not configured")));
        services.AddHttpClient(RaindropHttpClient).ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.raindrop.io/"));
        services.Configure<RaindropOptions>(Configuration.GetSection("Raindrop.io"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCookiePolicy();
        app.UseSession();
        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();
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
