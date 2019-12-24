using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SmallRss.Feeds
{
    public interface IFeedParser
    {
        Task<FeedParseResult> ParseAsync(Stream responseContent, CancellationToken cancellationToken);
    }
}