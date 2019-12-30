using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace SmallRss.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(@"smallrss.service.log", LogEventLevel.Verbose, fileSizeLimitBytes: 10_000_000,
                    rollOnFileSizeLimit: true, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1))
                .CreateLogger();

            var host = Host
                .CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .UseKestrel()
                    .UseStartup<Startup>()
                    .UseSerilog()
                )
                .Build();

            await host.RunAsync();
        }
    }
}
