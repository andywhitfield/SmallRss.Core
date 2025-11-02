using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmallRss.Data;
using SmallRss.Models;
using SmallRss.Web.Models.Manage;

namespace SmallRss.Web.Controllers;

[Authorize]
public class ManageController(ILogger<ManageController> logger,
    IUserAccountRepository userAccountRepository,
    IUserFeedRepository userFeedRepository,
    IRssFeedRepository rssFeedRepository,
    IHttpClientFactory clientFactory,
    IOptionsSnapshot<RaindropOptions> raindropOptions)
    : Controller
{
    [HttpGet]
    public async Task<ActionResult> Index() => View(await CreateIndexViewModelAsync());

    [HttpGet]
    public async Task<ActionResult> Edit(int id)
    {
        var vm = await CreateEditViewModelAsync(id);
        if (vm == null)
            return RedirectToAction(nameof(Index));

        return View(vm);
    }

    [HttpPost]
    public async Task<ActionResult> Delete(int id)
    {
        var userAccount = await userAccountRepository.GetAsync(User);
        var userFeeds = await userFeedRepository.GetAllByUserAsync(userAccount);

        var feed = userFeeds.FirstOrDefault(f => f.Id == id);
        if (feed == null)
            return RedirectToAction(nameof(Index));

        await userFeedRepository.RemoveAsync(feed);
        logger.LogInformation($"Removed feed: {feed.Id}:{feed.Name}");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<ActionResult> Refresh(int id)
    {
        var userAccount = await userAccountRepository.GetAsync(User);
        var userFeeds = await userFeedRepository.GetAllByUserAsync(userAccount);

        var feed = userFeeds.FirstOrDefault(f => f.Id == id);
        if (feed == null)
        {
            logger.LogWarning("UserFeed {Id} not found, nothing to do", id);
            return RedirectToAction(nameof(Index));
        }

        using var httpClient = clientFactory.CreateClient(Startup.DefaultHttpClient);
        HttpResponseMessage? response = null;
        try
        {
            logger.LogInformation("Refreshing feed {Id}", feed.RssFeedId);
            response = await httpClient.PostAsync($"/api/feed/refresh/{feed.RssFeedId}", new StringContent("{}", Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Refresh feed {Id}, status: {ResponseStatusCode}", feed.RssFeedId, response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not refresh feed {Id}, status: {ResponseStatusCode}", feed.RssFeedId, response?.StatusCode);
        }
        finally
        {
            response?.Dispose();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<ActionResult> AddAsync([FromForm] AddFeedViewModel addFeed)
    {
        if (!ModelState.IsValid || (string.IsNullOrWhiteSpace(addFeed.GroupSel) && string.IsNullOrWhiteSpace(addFeed.Group)))
        {
            var vm = await CreateIndexViewModelAsync();
            vm.Error = "Missing feed URL, group or name. Please complete all fields and try again.";
            return View(nameof(Index), vm);
        }

        var userAccount = await userAccountRepository.GetAsync(User);
        var rss = await GetOrCreateRssFeedAsync(addFeed.Url ?? "", userAccount.Id);
        if (rss == null)
        {
            var vm = await CreateIndexViewModelAsync();
            vm.Error = "Could not create feed, please try again.";
            return View(nameof(Index), vm);
        }

        await userFeedRepository.CreateAsync(
            rss.Id,
            userAccount.Id,
            addFeed.Name ?? "",
            (string.IsNullOrWhiteSpace(addFeed.GroupSel) ? addFeed.Group : addFeed.GroupSel) ?? "");

        logger.LogInformation($"Created new user feed: {addFeed.Name} - {addFeed.Url}");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<ActionResult> Save([FromForm] SaveFeedViewModel saveFeed)
    {
        if (!ModelState.IsValid || (string.IsNullOrWhiteSpace(saveFeed.GroupSel) && string.IsNullOrWhiteSpace(saveFeed.Group)))
        {
            var vm = await CreateEditViewModelAsync(saveFeed.Id);
            if (vm == null)
                return RedirectToAction(nameof(Index));

            vm.Error = "Could not update feed due to a missing feed URL, group or name. Please complete all fields and try again.";
            return View(nameof(Edit), vm);
        }

        var userAccount = await userAccountRepository.GetAsync(User);
        var rss = await GetOrCreateRssFeedAsync(saveFeed.Url ?? "", userAccount.Id);
        var feed = await userFeedRepository.GetByIdAsync(saveFeed.Id);
        if (rss == null || feed == null || feed.UserAccountId != userAccount.Id)
        {
            var vm = await CreateEditViewModelAsync(saveFeed.Id);
            if (vm == null)
                return RedirectToAction(nameof(Index));

            vm.Error = "Could not update feed due to a security check failure. Please try again";
            return View(nameof(Edit), vm);
        }

        await rssFeedRepository.UpdateDecodeBodyAsync(rss.Id, !string.IsNullOrEmpty(saveFeed.Decode));

        feed.GroupName = (string.IsNullOrWhiteSpace(saveFeed.GroupSel) ? saveFeed.Group : saveFeed.GroupSel) ?? "";
        feed.Name = saveFeed.Name ?? "";
        feed.RssFeedId = rss.Id;
        await userFeedRepository.UpdateAsync(feed);

        logger.LogInformation($"Updated user feed: {saveFeed.Name}");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<ActionResult> Raindrop()
    {
        var userAccount = await userAccountRepository.GetAsync(User);
        if (userAccount.HasRaindropRefreshToken)
        {
            // disconnect from raindrop requested
            userAccount.RaindropRefreshToken = string.Empty;
            await userAccountRepository.UpdateAsync(userAccount);
            return RedirectToAction(nameof(Index));
        }

        var raindropAuthUri = $"https://api.raindrop.io/v1/oauth/authorize?client_id={raindropOptions.Value.ClientId}&redirect_uri={HttpUtility.UrlEncode(RaindropDirectUri)}";
        logger.LogInformation($"Redirecting to raindrop: {raindropAuthUri}");
        return Redirect(raindropAuthUri);
    }

    [HttpGet]
    public async Task<ActionResult> RaindropAuth(string code)
    {
        logger.LogInformation($"Received raindrop.io auth code: {code}");
        if (string.IsNullOrEmpty(code))
            return RedirectToAction("Index");

        logger.LogInformation($"Getting authorization_code from raindrop.io: code={code}, client_id={raindropOptions.Value.ClientId}");
        using var raindropClient = clientFactory.CreateClient(Startup.RaindropHttpClient);

        var requestJson = JsonSerializer.Serialize(new { code, client_id = raindropOptions.Value.ClientId, client_secret = raindropOptions.Value.ClientSecret, grant_type = "authorization_code", redirect_uri = RaindropDirectUri });
        using var response = await raindropClient.PostAsync("https://raindrop.io/oauth/access_token",
            new StringContent(requestJson, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Invalid raindrop response: {response.StatusCode}");

        var result = await response.Content.ReadAsStringAsync();
        if (!result.TryParseJson(out RaindropTokenResult? authResult, logger))
            return RedirectToAction("Index");

        logger.LogInformation($"Got token result: result={result}");

        // save refresh token into the user's account
        var userAccount = await userAccountRepository.GetAsync(User);
        userAccount.RaindropRefreshToken = authResult?.RefreshToken ?? "";
        await userAccountRepository.UpdateAsync(userAccount);

        return RedirectToAction(nameof(Index));
    }

    private string RaindropDirectUri => Url.Action(nameof(RaindropAuth), "Manage", null, Request.Scheme) ?? throw new InvalidOperationException("Cannot create raindrop.io redirect uri");

    private async Task<RssFeed?> GetOrCreateRssFeedAsync(string feedUri, int userAccountId)
    {
        var rss = await rssFeedRepository.GetByUriAsync(feedUri);
        if (rss == null)
        {
            using var httpClient = clientFactory.CreateClient(Startup.DefaultHttpClient);
            var jsonRequest = JsonSerializer.Serialize(new { Uri = feedUri, UserAccountId = userAccountId });
            using var response = await httpClient.PostAsync("/api/feed/create", new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode || !responseJson.TryParseJson(out CreateRssFeedResponse? createRssFeedResult, logger))
            {
                logger.LogError($"Could not create feed: response code {response.StatusCode}: content: {responseJson}");
                return null;
            }
            logger.LogTrace($"Received response - location:{response.Headers.Location} content:{responseJson}");

            rss = new RssFeed { Id = createRssFeedResult?.Id ?? 0 };

            logger.LogInformation($"Created new RSS feed: {feedUri} Id: {rss.Id}");
        }
        return rss;
    }

    private async Task<IndexViewModel> CreateIndexViewModelAsync()
    {
        var userAccount = await userAccountRepository.GetAsync(User);
        var userFeeds = await userFeedRepository.GetAllByUserAsync(userAccount);
        var rssFeeds = await rssFeedRepository.GetByIdsAsync(userFeeds.Select(uf => uf.RssFeedId));
        return new IndexViewModel { UserAccount = userAccount, Feeds = userFeeds.Select(f => new FeedSubscriptionViewModel(f, rssFeeds.Single(rf => rf.Id == f.RssFeedId))).OrderBy(f => f.Name).OrderBy(f => f.Group) };
    }

    private async Task<EditViewModel?> CreateEditViewModelAsync(int userFeedId)
    {
        var userAccount = await userAccountRepository.GetAsync(User);
        var userFeeds = await userFeedRepository.GetAllByUserAsync(userAccount);

        var userFeed = userFeeds.FirstOrDefault(uf => uf.Id == userFeedId);
        if (userFeed == null)
            return null;

        var rss = await rssFeedRepository.GetByIdAsync(userFeed.RssFeedId);
        if (rss == null)
            return null;

        return new EditViewModel { Feed = new FeedSubscriptionViewModel(userFeed, rss), CurrentGroups = userFeeds.Select(f => f.GroupName ?? "").Distinct().OrderBy(g => g) };
    }

    private class RequestToken
    {
        public string? Code { get; set; }
    }

    private class RaindropTokenResult
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }

    private class CreateRssFeedResponse
    {
        public int Id { get; set; }
    }
}
