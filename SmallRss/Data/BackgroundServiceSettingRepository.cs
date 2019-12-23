using Microsoft.Extensions.Logging;

namespace SmallRss.Data
{
    public class BackgroundServiceSettingRepository : IBackgroundServiceSettingRepository
    {
        private readonly ILogger<BackgroundServiceSettingRepository> _logger;
        private readonly SqliteDataContext _context;

        public BackgroundServiceSettingRepository(ILogger<BackgroundServiceSettingRepository> logger, SqliteDataContext context)
        {
            _logger = logger;
            _context = context;
        }

    }
}