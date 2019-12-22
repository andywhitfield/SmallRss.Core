﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SmallRss.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
