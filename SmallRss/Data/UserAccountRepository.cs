using System;
using System.Linq;
using System.Security.Claims;

namespace SmallRss.Data
{
    public class UserAccountRepository : IUserAccountRepository
    {
        /*
        private readonly SqliteDataContext context;

        public UserAccountRepository(SqliteDataContext context)
        {
            this.context = context;
        }

        public bool HasMasterPassword(ClaimsPrincipal user)
        {
            return TryGetAccount(user, out var userAccount) &&
                !string.IsNullOrWhiteSpace(userAccount.PasswordDatabase);
        }

        public void CreateNewUser(ClaimsPrincipal user, string masterPassword)
        {
            var authenticationUri = GetIdentifierFromPrincipal(user);
            var newAccount = new UserAccount { AuthenticationUri = authenticationUri };

            using (var secure = new Secure())
            {
                newAccount.SetPasswordDb(new PasswordDb {
                    MasterPassword = secure.HashValue(masterPassword)
                });
            }

            context.UserAccounts.Add(newAccount);
            context.SaveChanges();
        }

        private string GetIdentifierFromPrincipal(ClaimsPrincipal user)
        {
            return user?.FindFirstValue("sub");
        }

        public UserAccount GetUserAccount(ClaimsPrincipal user)
        {
            return TryGetAccount(user, out var account) ? account : throw new ArgumentException($"No UserAccount for the user: {user}");
        }

        private bool TryGetAccount(ClaimsPrincipal user, out UserAccount userAccount)
        {
            var authenticationUri = GetIdentifierFromPrincipal(user);
            if (string.IsNullOrWhiteSpace(authenticationUri))
            {
                userAccount = default(UserAccount);
                return false;
            }

            userAccount = context.UserAccounts.FirstOrDefault(ua => ua.AuthenticationUri == authenticationUri);
            return userAccount != null;
        }

        public void SaveUserAccount(UserAccount userAccount)
        {
            context.UserAccounts.Update(userAccount);
            context.SaveChanges();
        }
        */
    }
}