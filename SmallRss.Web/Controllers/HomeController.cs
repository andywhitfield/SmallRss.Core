using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallRss.Data;
using SmallRss.Web.Models.Home;

namespace SmallRss.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserAccountRepository userAccountRepository;

        public HomeController(IUserAccountRepository userAccountRepository)
        {
            this.userAccountRepository = userAccountRepository;
        }
        
        [Authorize]
        public IActionResult Index()
        {
            /*
            if (!userAccountRepository.HasMasterPassword(User))
            {
                return Redirect("~/newuser");
            }
            */

            return View(new IndexViewModel());
        }

        public IActionResult Error() => View();
    }
}
