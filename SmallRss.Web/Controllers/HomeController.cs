using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Web.Models.Home;

namespace SmallRss.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        
        [Authorize]
        public IActionResult Index()
        {
            var userId = (User.Identity as ClaimsIdentity)?.FindFirst("sub")?.Value ?? User.Identity.Name;
            _logger.LogInformation($"Logged in {userId}");
            
            return View(new IndexViewModel());
        }

        public IActionResult Error() => View();
    }
}
