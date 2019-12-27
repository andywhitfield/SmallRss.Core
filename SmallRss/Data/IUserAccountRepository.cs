using System.Security.Claims;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IUserAccountRepository
    {
        Task<UserAccount> FindOrCreateAsync(ClaimsPrincipal user);
        Task UpdateAsync(UserAccount userAccount);
    }
}