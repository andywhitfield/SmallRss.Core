using System.Threading.Tasks;
using System.Xml.Linq;

namespace SmallRss.Feeds
{
    public interface IFeedReader
    {
        bool CanRead(XDocument doc);
        Task<FeedParseResult> ReadAsync(XDocument doc);
    }
}