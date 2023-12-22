using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmallRss.Models;

public class UserAccount
{
    public UserAccount()
    {
        Email = "";
        UserAccountCredentials = new List<UserAccountCredential>();
        ShowAllItems = false;
        ExpandedGroups = new HashSet<string>();
        SavedLayout = new Dictionary<string, string>();
        PocketAccessToken = "";
        RaindropRefreshToken = "";
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
    public string PocketAccessToken { get; set; }
    [NotMapped]
    public bool HasPocketAccessToken => !string.IsNullOrWhiteSpace(PocketAccessToken);
    [NotMapped]
    public string RaindropRefreshToken { get; set; }
    [NotMapped]
    public bool HasRaindropRefreshToken => !string.IsNullOrWhiteSpace(RaindropRefreshToken);
}
