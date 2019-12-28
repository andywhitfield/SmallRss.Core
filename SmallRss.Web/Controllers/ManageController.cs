using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Models;
using SmallRss.Web.Models.Manage;

namespace SmallRss.Web.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        internal const string PocketConsumerKey = "41619-1a5decf504173a588fd1b492";
        private readonly ILogger<ManageController> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IUserFeedRepository _userFeedRepository;
        private readonly IRssFeedRepository _rssFeedRepository;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _serviceUri;

        public ManageController(ILogger<ManageController> logger,
            IUserAccountRepository userAccountRepository,
            IUserFeedRepository userFeedRepository,
            IRssFeedRepository rssFeedRepository,
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _userFeedRepository = userFeedRepository;
            _rssFeedRepository = rssFeedRepository;
            _clientFactory = clientFactory;
            _serviceUri = configuration.GetValue<string>("ServiceUri");
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return View(await CreateIndexViewModelAsync());
        }

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
            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            var userFeeds = await _userFeedRepository.GetAllByUserAsync(userAccount);

            var feed = userFeeds.FirstOrDefault(f => f.Id == id);
            if (feed == null)
                return RedirectToAction(nameof(Index));

            await _userFeedRepository.RemoveAsync(feed);
            _logger.LogInformation($"Removed feed: {feed.Id}:{feed.Name}");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<ActionResult> AddAsync([FromForm]AddFeedViewModel addFeed)
        {
            if (!ModelState.IsValid || (string.IsNullOrWhiteSpace(addFeed.GroupSel) && string.IsNullOrWhiteSpace(addFeed.Group)))
            {
                var vm = await CreateIndexViewModelAsync();
                vm.Error = "Missing feed URL, group or name. Please complete all fields and try again.";
                return View(nameof(Index), vm);
            }

            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            var rss = await _rssFeedRepository.GetByUriAsync(addFeed.Url);
            if (rss == null)
            {
                using var httpClient = _clientFactory.CreateClient();
                var jsonRequest = JsonSerializer.Serialize(new { Uri = addFeed.Url, UserAccountId = userAccount.Id });
                using var response = await httpClient.PostAsync($"{_serviceUri}/api/feed/create", new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
                var responseJson = await response.Content?.ReadAsStringAsync();
                CreateRssFeedResponse createRssFeedResult = null;
                if (!response.IsSuccessStatusCode || !responseJson.TryParseJson(out createRssFeedResult, _logger))
                {
                    _logger.LogError($"Could not create feed: response code {response.StatusCode}: content: {responseJson}");
                    var vm = await CreateIndexViewModelAsync();
                    vm.Error = "Could not create feed, please try again.";
                    return View(nameof(Index), vm);                    
                }
                rss = new RssFeed { Id = createRssFeedResult.RssFeedId };

                _logger.LogInformation($"Created new RSS feed: {addFeed.Url} Id: {rss.Id}");
            }

            await _userFeedRepository.CreateAsync(
                rss.Id,
                userAccount.Id,
                addFeed.Name,
                string.IsNullOrWhiteSpace(addFeed.GroupSel) ? addFeed.Group : addFeed.GroupSel);

            _logger.LogInformation($"Created new user feed: {addFeed.Name} - {addFeed.Url}");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public ActionResult Save([FromForm]SaveFeedViewModel saveFeed)
        {
            /*
            TODO
            var user = this.CurrentUser(datastore);
            if (!ModelState.IsValid || (string.IsNullOrWhiteSpace(saveFeed.GroupSel) && string.IsNullOrWhiteSpace(saveFeed.Group)))
            {
                var vm = CreateEditViewModel(user, saveFeed.Id);
                if (vm == null)
                    return RedirectToAction("index");

                vm.Error = "Could not update feed due to a missing feed URL, group or name. Please complete all fields and try again.";
                return View("Edit", vm);
            }

            var rss = datastore.LoadAll<RssFeed>("Uri", saveFeed.Url).FirstOrDefault();
            if (rss == null)
            {
                rss = datastore.Store(new RssFeed { Uri = saveFeed.Url });
                log.InfoFormat("Updating user feed, created new rss: {0}", saveFeed.Url);
            }

            var feed = datastore.Load<UserFeed>(saveFeed.Id);
            if (feed == null || feed.UserAccountId != user.Id)
            {
                var vm = CreateEditViewModel(user, saveFeed.Id);
                if (vm == null)
                    return RedirectToAction("index");

                vm.Error = "Could not update feed due to a security check failure. Please try again";
                return View("Edit", vm);
            }

            feed.GroupName = string.IsNullOrWhiteSpace(saveFeed.GroupSel) ? saveFeed.Group : saveFeed.GroupSel;
            feed.Name = saveFeed.Name;
            feed.RssFeedId = rss.Id;
            datastore.Update(feed);

            log.InfoFormat("Updating user feed: {0}", saveFeed.Name);
            */

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<ActionResult> Pocket()
        {
            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            if (userAccount.HasPocketAccessToken)
            {
                // disconnect from pocket requested
                userAccount.PocketAccessToken = string.Empty;
                await _userAccountRepository.UpdateAsync(userAccount);
                return RedirectToAction(nameof(Index));
            }

            var redirectUri = Url.Action(nameof(PocketAuth), "Manage", null, Request.Scheme);
            var requestJson = "{\"consumer_key\":\"" + PocketConsumerKey + "\", \"redirect_uri\":\"" + HttpUtility.UrlEncode(redirectUri) + "\"}";

            using var webClient = new WebClient();
            webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json; charset=UTF-8");
            webClient.Headers.Add("X-Accept", "application/json");
            var result = await webClient.UploadStringTaskAsync("https://getpocket.com/v3/oauth/request", requestJson);
            if (!result.TryParseJson(out RequestToken requestToken, _logger))
                throw new InvalidOperationException($"Cannot deserialize response: {result}");

            // parse result: {"code":"f5efd910-9415-7fb1-a1f7-981402","state":null}
            HttpContext.Session.SetString("POCKET_CODE", requestToken.Code);
            return Redirect($"https://getpocket.com/auth/authorize?request_token={HttpUtility.UrlEncode(requestToken.Code)}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}");
        }

        public async Task<ActionResult> PocketAuth()
        {
            var code = HttpContext.Session.GetString("POCKET_CODE");
            var requestJson = JsonSerializer.Serialize(new { consumer_key = PocketConsumerKey, code });

            using var webClient = new WebClient();
            webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json; charset=UTF-8");
            webClient.Headers.Add("X-Accept", "application/json");
            var result = await webClient.UploadStringTaskAsync("https://getpocket.com/v3/oauth/authorize", requestJson);
            if (!result.TryParseJson(out PocketAuthResult authResult, _logger))
                return RedirectToAction("Index");

            // save access token into the user's account
            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            userAccount.PocketAccessToken = authResult.AccessToken;
            await _userAccountRepository.UpdateAsync(userAccount);

            return RedirectToAction(nameof(Index));
        }

        private async Task<IndexViewModel> CreateIndexViewModelAsync()
        {
            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            var userFeeds = await _userFeedRepository.GetAllByUserAsync(userAccount);
            var rssFeeds = await _rssFeedRepository.GetByIdsAsync(userFeeds.Select(uf => uf.RssFeedId));
            return new IndexViewModel { UserAccount = userAccount, Feeds = userFeeds.Select(f => new FeedSubscriptionViewModel(f, rssFeeds.Single(rf => rf.Id == f.RssFeedId))).OrderBy(f => f.Name).OrderBy(f => f.Group) };
        }

        private async Task<EditViewModel> CreateEditViewModelAsync(int userFeedId)
        {
            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            var userFeeds = await _userFeedRepository.GetAllByUserAsync(userAccount);

            var userFeed = userFeeds.FirstOrDefault(uf => uf.Id == userFeedId);
            if (userFeed == null)
                return null;

            var rss = await _rssFeedRepository.GetByIdAsync(userFeed.RssFeedId);
            return new EditViewModel { Feed = new FeedSubscriptionViewModel(userFeed, rss), CurrentGroups = userFeeds.Select(f => f.GroupName).Distinct().OrderBy(g => g) };
        }

        private class RequestToken
        {
            public string Code { get; set; }
        }

        private class PocketAuthResult
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
            public string Username { get; set; }
        }

        private class CreateRssFeedResponse
        {
            public int RssFeedId { get; set; }
        }
    }
}
