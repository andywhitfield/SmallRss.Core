using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmallRss.Models
{
    public class UserAccount
    {
        public UserAccount()
        {
            AuthenticationIds = new HashSet<string>();
            ShowAllItems = false;
            ExpandedGroups = new HashSet<string>();
            SavedLayout = new Dictionary<string, string>();
            PocketAccessToken = string.Empty;
        }

        public int Id { get; set; }
        public DateTime? LastLogin { get; set; }

        [NotMapped]
        public ISet<string> AuthenticationIds { get; private set; }
        [NotMapped]
        public bool ShowAllItems { get; set; }
        [NotMapped]
        public ISet<string> ExpandedGroups { get; private set; }
        [NotMapped]
        public IDictionary<string, string> SavedLayout { get; private set; }
        [NotMapped]
        public string PocketAccessToken { get; set; }
        [NotMapped]
        public bool HasPocketAccessToken { get { return !string.IsNullOrWhiteSpace(PocketAccessToken); } }
    }
}
