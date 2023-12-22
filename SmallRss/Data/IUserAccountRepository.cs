using System.Security.Claims;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data;

public interface IUserAccountRepository
{
    Task<UserAccount> GetAsync(ClaimsPrincipal user);
    Task<UserAccount?> FindAsync(ClaimsPrincipal user);
    Task<UserAccount> CreateAsync(string email, byte[] credentialId, byte[] publicKey, byte[] userHandle);
    Task UpdateAsync(UserAccount userAccount);
    Task<UserAccount?> FindByEmailAsync(string email);
    Task<UserAccount?> FindByUserHandleAsync(byte[] userHandle);
}