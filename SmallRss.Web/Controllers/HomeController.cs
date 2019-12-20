using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Web.Models.Home;

namespace SmallRss.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly IUserAccountRepository userAccountRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IUserAccountRepository userAccountRepository)
        {
            this.logger = logger;
            this.userAccountRepository = userAccountRepository;
        }
        
        [Authorize]
        public IActionResult Index()
        {
            var userId = (User.Identity as ClaimsIdentity)?.FindFirst("sub")?.Value ?? User.Identity.Name;
            logger.LogInformation($"Logged in {userId}");
            
            return View(new IndexViewModel());
        }

        public IActionResult Error() => View();
    }
}
