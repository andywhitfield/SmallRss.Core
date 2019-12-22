using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmallRss.Models;

namespace SmallRss.Data
{
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly SqliteDataContext _context;

        public UserAccountRepository(SqliteDataContext context)
        {
            _context = context;
        }

        public async Task<UserAccount> FindByUserPrincipalAsync(ClaimsPrincipal user)
        {
            var userIdentifier = user?.FindFirst("sub")?.Value;
            if (userIdentifier == null)
                return null;

            var userAccountSetting = await _context.UserAccountSettings.SingleOrDefaultAsync(uas =>
                uas.SettingType == "AuthenticationId" &&
                uas.SettingType == userIdentifier);
            if (userAccountSetting == null)
                return null;
            
            return await GetByIdAsync(userAccountSetting.UserAccountId);
        }

        public async Task<UserAccount> GetByIdAsync(int userAccountId)
        {
            return null;
        }
    }
}