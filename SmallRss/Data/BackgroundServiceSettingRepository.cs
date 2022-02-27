using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

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

        public Task<BackgroundServiceSetting?> FindSettingByNameAsync(string settingName) =>
            _context.BackgroundServiceSettings!.Where(bss => bss.SettingName == settingName).FirstOrDefaultAsync();

        public async Task AddOrUpdateAsync(string settingName, string settingValue)
        {
            var currentSetting = await FindSettingByNameAsync(settingName);
            if (currentSetting != null)
                currentSetting.SettingValue = settingValue;
            else
                await _context.BackgroundServiceSettings!.AddAsync(new BackgroundServiceSetting
                {
                    SettingName = settingName,
                    SettingValue = settingValue
                });
            await _context.SaveChangesAsync();
        }

        public Task<List<BackgroundServiceSetting>> GetAllAsync() =>
            _context.BackgroundServiceSettings!.ToListAsync();
    }
}