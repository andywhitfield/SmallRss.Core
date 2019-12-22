using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SmallRss.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
