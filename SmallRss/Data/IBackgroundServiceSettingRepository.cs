using System.Collections.Generic;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IBackgroundServiceSettingRepository
    {
        Task<List<BackgroundServiceSetting>> GetAllAsync();
        Task<BackgroundServiceSetting?> FindSettingByNameAsync(string settingName);
        Task AddOrUpdateAsync(string settingName, string settingValue);
    }
}