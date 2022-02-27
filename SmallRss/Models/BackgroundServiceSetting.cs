using System.ComponentModel.DataAnnotations;

namespace SmallRss.Models
{
    public class BackgroundServiceSetting
    {
        public int Id { get; set; }
        [Required]
        public string? SettingName { get; set; }
        public string? SettingValue { get; set; }
    }
}
