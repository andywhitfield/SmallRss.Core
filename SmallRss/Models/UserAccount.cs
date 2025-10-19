using System.ComponentModel.DataAnnotations.Schema;

namespace SmallRss.Models;

public class UserAccount
{
    public UserAccount()
    {
        Email = "";
        UserAccountCredentials = [];
        ShowAllItems = false;
        ExpandedGroups = new HashSet<string>();
        SavedLayout = new Dictionary<string, string>();
        RaindropRefreshToken = "";
        AllUnreadSortOrder = "";
    }

    public int Id { get; set; }
    public DateTime? LastLogin { get; set; }

    [NotMapped]
    public string Email { get; set; }
    [NotMapped]
    public IList<UserAccountCredential> UserAccountCredentials { get; private set; }
    [NotMapped]
    public bool ShowAllItems { get; set; }
    [NotMapped]
    public ISet<string> ExpandedGroups { get; private set; }
    [NotMapped]
    public IDictionary<string, string> SavedLayout { get; private set; }
    [NotMapped]
    public string RaindropRefreshToken { get; set; }
    [NotMapped]
    public bool HasRaindropRefreshToken => !string.IsNullOrWhiteSpace(RaindropRefreshToken);
    [NotMapped]
    public string AllUnreadSortOrder { get; set; }
}
