using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallRss.Data;
using SmallRss.Web.Models.Home;

namespace SmallRss.Web.Controllers;

public class HomeController(IUserAccountRepository userAccountRepository) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userAccount = await userAccountRepository.GetAsync(User);
        return View(new IndexViewModel {
            ShowAllArticles = userAccount.ShowAllItems,
            ConnectedToSave = userAccount.HasRaindropRefreshToken
        });
    }

    public IActionResult Error() => View();
}
