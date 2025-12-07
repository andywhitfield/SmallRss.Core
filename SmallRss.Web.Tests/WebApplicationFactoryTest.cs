using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;

[assembly: Parallelize]

namespace SmallRss.Web.Tests;

public class WebApplicationFactoryTest : WebApplicationFactory<Startup>
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SqliteDataContext> _options;
    private UserAccount? _testUser;

    public WebApplicationFactoryTest()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<SqliteDataContext>().UseSqlite(_connection).Options;
    }

    public UserAccount TestUser => _testUser ?? throw new InvalidOperationException("Test user not created");

    protected override IHostBuilder CreateHostBuilder()
        => Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(x => x.UseStartup<Startup>().UseTestServer().ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Scoped(sp => new SqliteDataContext(sp.GetRequiredService<ILogger<SqliteDataContext>>(), _options)));
            services
                .AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestStubAuthHandler>("Test", null);
        }));

    public async Task<HttpClient> CreateUnauthenticatedClientAsync(bool allowAutoRedirect = false)
    {
        await CreateTestUserAsync();
        return CreateClient();
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(bool allowAutoRedirect = true)
    {
        await CreateTestUserAsync();
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("TESTAUTH", "true");
        return client;
    }

    public async Task CreateTestUserAsync()
    {
        if (_testUser != null)
            return;

        await using var serviceScope = Services.CreateAsyncScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        _testUser = context.UserAccounts!.Add(new() { Email = TestStubAuthHandler.TestUserEmail }).Entity;
        await context.SaveChangesAsync();
        context.UserAccountSettings!.Add(new() { SettingType = "Email", SettingName = "Email", SettingValue = TestStubAuthHandler.TestUserEmail, UserAccountId = _testUser.Id  });
        await context.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}
