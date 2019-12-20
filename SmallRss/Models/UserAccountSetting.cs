using System.ComponentModel.DataAnnotations;

namespace SmallRss.Models
{
    public class UserAccountSetting
    {
        public int Id { get; set; }
        public int UserAccountId { get; set; }
        [Required]
        public string SettingType { get; set; }
        [Required]
        public string SettingName { get; set; }
        public string SettingValue { get; set; }
    }
}
