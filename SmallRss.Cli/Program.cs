using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmallRss.Cli
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SmallRss.Cli.exe <feed uri>");
                return;
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", args.Length > 1 ? args[1] : "Mozilla/5.0 (Windows)");

                using var response = await client.GetAsync(args[0]);
                var responseContent = await response.Content?.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    Console.WriteLine($"Could not download feed [{args[0]}]: response status: {response.StatusCode}, content: {responseContent}");
                else
                    Console.WriteLine($"Downloaded feed [{args[0]}]: response status: {response.StatusCode}, content: {responseContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }
    }
}