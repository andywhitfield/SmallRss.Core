﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallRss.Data;
using SmallRss.Web.Models.Home;

namespace SmallRss.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUserAccountRepository _userAccountRepository;

    public HomeController(IUserAccountRepository userAccountRepository) =>
        _userAccountRepository = userAccountRepository;

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
        return View(new IndexViewModel {
            ShowAllArticles = userAccount.ShowAllItems,
            ConnectedToSave = userAccount.HasPocketAccessToken || userAccount.HasRaindropRefreshToken
        });
    }

    public IActionResult Error() => View();
}
