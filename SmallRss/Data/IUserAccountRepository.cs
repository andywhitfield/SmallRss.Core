using System.Security.Claims;
using System.Threading.Tasks;
using SmallRss.Models;

namespace SmallRss.Data
{
    public interface IUserAccountRepository
    {
        Task<UserAccount> FindByUserPrincipalAsync(ClaimsPrincipal user);
    }
}