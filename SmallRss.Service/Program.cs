using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using Serilog.Events;

namespace SmallRss.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var logPath = ".";
            if (WindowsServiceHelpers.IsWindowsService())
                logPath = AppContext.BaseDirectory;
            logPath = Path.Combine(logPath, "smallrss.service.log");

            const string logOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: logOutputTemplate)
                .WriteTo.File(logPath, LogEventLevel.Verbose, outputTemplate: logOutputTemplate,
                    fileSizeLimitBytes: 10_000_000, rollOnFileSizeLimit: true, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1))
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
