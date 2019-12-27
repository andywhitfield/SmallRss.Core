using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Web.Models.Home;

namespace SmallRss.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserAccountRepository _userAccountRepository;

        public HomeController(ILogger<HomeController> logger, IUserAccountRepository userAccountRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
        }
        
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            return View(new IndexViewModel {
                ShowAllArticles = userAccount.ShowAllItems,
                ConnectedToPocket = userAccount.HasPocketAccessToken
            });
        }

        public IActionResult Error() => View();
    }
}
