using Microsoft.Extensions.Logging;
using SmallRss.Feeds;

namespace SmallRss.Cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: SmallRss.Cli.exe -uri <feed uri> [user agent] or SmallRss.Cli.exe -parse <rss file>");
            return;
        }

        if (string.Equals(args[0], "-uri", StringComparison.OrdinalIgnoreCase))
            await DownloadFromUriAsync(args[1], args.Length > 2 ? args[2] : "Mozilla/5.0 (Windows)");
        else if (string.Equals(args[0], "-parse", StringComparison.OrdinalIgnoreCase))
            await ParseRssFileAsync(args[1]);
    }

    private static async Task DownloadFromUriAsync(string uri, string userAgent)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            using var response = await client.GetAsync(uri);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Could not download feed [{uri}]: response status: {response.StatusCode}, content: {responseContent}");
            else
                Console.WriteLine($"Downloaded feed [{uri}]: response status: {response.StatusCode}, content: {responseContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
        }        
    }

    private static async Task ParseRssFileAsync(string rssFile)
    {
        LoggerFactory loggerFactory = new();
        FeedParser feedParser = new(loggerFactory.CreateLogger<FeedParser>(), [new AtomFeedReader(loggerFactory.CreateLogger<AtomFeedReader>()), new RssFeedReader(loggerFactory.CreateLogger<RssFeedReader>())]);
        FeedParseResult parseResult;
        await using var fileStream = File.OpenRead(rssFile);
        if (!((parseResult = await feedParser.ParseAsync(fileStream, CancellationToken.None))?.IsValid ?? false))
        {
            Console.WriteLine("Could not parse rss");
            return;
        }
        
        foreach (var itemInFeed in parseResult.Articles)
        {
            Console.WriteLine("Item: published=[{0}] heading=[{1}] author=[{2}] url=[{3}] body=[{4}]",
                itemInFeed.Published, itemInFeed.Heading, itemInFeed.Author, itemInFeed.Url, itemInFeed.Body);
        }
    }
}
