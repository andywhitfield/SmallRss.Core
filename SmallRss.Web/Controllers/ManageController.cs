using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Web.Models.Manage;

namespace SmallRss.Web.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        internal const string PocketConsumerKey = "41619-1a5decf504173a588fd1b492";
        private readonly ILogger<ManageController> _logger;

        public ManageController(ILogger<ManageController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View(CreateIndexViewModel());
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var vm = CreateEditViewModel(id);
            if (vm == null)
                return RedirectToAction("index");

            return View(vm);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            /*
            TODO
            var user = this.CurrentUser(datastore);
            var feeds = datastore.LoadAll<UserFeed>("UserAccountId", user.Id);

            var feed = feeds.FirstOrDefault(f => f.Id == id);
            if (feed == null)
                return RedirectToAction("index");

            var removeCount = datastore.RemoveUserArticleRead(user, feed);
            log.InfoFormat("Removed {0} user article read records: {1}:{2}", removeCount, feed.Id, feed.Name);

            removeCount = datastore.Remove(feed);
            log.InfoFormat("Removed {0} feed: {1}:{2}", removeCount, feed.Id, feed.Name);
            */

            return RedirectToAction("index");
        }

        [HttpPost]
        public ActionResult Add([FromForm]AddFeedViewModel addFeed)
        {
            /*
            var user = this.CurrentUser(datastore);

            if (!ModelState.IsValid || (string.IsNullOrWhiteSpace(addFeed.GroupSel) && string.IsNullOrWhiteSpace(addFeed.Group)))
            {
                var vm = CreateIndexViewModel(user);
                vm.Error = "Missing feed URL, group or name. Please complete all fields and try again.";
                return View("Index", vm);
            }

            var rss = datastore.LoadAll<RssFeed>("Uri", addFeed.Url).FirstOrDefault();
            if (rss == null)
            {
                rss = datastore.Store(new RssFeed { Uri = addFeed.Url });
                log.InfoFormat("Created new RSS feed: {0}", addFeed.Url);
                serviceApi.RefreshFeed(user.Id, rss.Id);
            }

            var newFeed = new UserFeed {
                GroupName = string.IsNullOrWhiteSpace(addFeed.GroupSel) ? addFeed.Group : addFeed.GroupSel,
                Name = addFeed.Name,
                RssFeedId = rss.Id,
                UserAccountId = user.Id
            };
            datastore.Store(newFeed);

            log.InfoFormat("Created new user feed: {0} - {1}", addFeed.Name, addFeed.Url);
            */

            return RedirectToAction("index");
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

            return RedirectToAction("index");
        }

        [HttpPost]
        public ActionResult Pocket()
        {
            return RedirectToAction("Index");
            /*
            TODO
            var userAccount = this.CurrentUser(datastore);
            if (userAccount.HasPocketAccessToken)
            {
                // disconnect from pocket requested
                userAccount.PocketAccessToken = string.Empty;
                datastore.UpdateAccount(userAccount);
                return RedirectToAction("Index");
            }

            var redirectUri = Url.Action("PocketAuth", "Manage", routeValues: null, protocol: Request.Url.Scheme);
            var requestJson = "{\"consumer_key\":\"" + PocketConsumerKey + "\", \"redirect_uri\":\"" + Url.Encode(redirectUri) + "\"}";

            var webClient = new WebClient();
            webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json; charset=UTF-8");
            webClient.Headers.Add("X-Accept", "application/json");
            var result = webClient.UploadString("https://getpocket.com/v3/oauth/request", requestJson);

            var jsonDeserializer = new DataContractJsonSerializer(typeof(RequestToken));
            var requestToken = jsonDeserializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(result))) as RequestToken;
            if (requestToken == null)
            {
                throw new InvalidOperationException("Cannot deserialize response: " + result);
            }
            // parse result: {"code":"f5efd910-9415-7fb1-a1f7-981402","state":null}
            Session["POCKET_CODE"] = requestToken.code;
            var redirectToPocket = string.Format("https://getpocket.com/auth/authorize?request_token={0}&redirect_uri={1}", Url.Encode(requestToken.code), Url.Encode(redirectUri));

            return Redirect(redirectToPocket);
            */
        }

        public ActionResult PocketAuth()
        {
            var code = this.HttpContext.Session.GetString("POCKET_CODE");
            var requestJson = JsonSerializer.Serialize(new { consumer_key = PocketConsumerKey, code });

            var webClient = new WebClient();
            webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json; charset=UTF-8");
            webClient.Headers.Add("X-Accept", "application/json");
            var result = webClient.UploadString("https://getpocket.com/v3/oauth/authorize", requestJson);
            if (!result.TryParseJson(out PocketAuthResult authResult, _logger))
                return RedirectToAction("Index");

            /*
            // save access token into the user's account
            var userAccount = this.CurrentUser(datastore);
            userAccount.PocketAccessToken = authResult.access_token;
            datastore.UpdateAccount(userAccount);
            */

            return RedirectToAction("Index");
        }

        private IndexViewModel CreateIndexViewModel()
        {
            /*
            var feeds = datastore.LoadUserRssFeeds(user.Id);
            return new IndexViewModel { UserAccount = user, Feeds = feeds.Select(f => new FeedSubscriptionViewModel(f.Item1, f.Item2)).OrderBy(f => f.Name).OrderBy(f => f.Group) };
            */
            return new IndexViewModel();
        }

        private EditViewModel CreateEditViewModel(int feedId)
        {
            /*
            var feeds = datastore.LoadAll<UserFeed>("UserAccountId", user.Id);
            var feed = feeds.FirstOrDefault(f => f.Id == feedId);
            if (feed == null)
                return null;

            var rss = datastore.Load<RssFeed>(feed.RssFeedId);
            return new EditViewModel { Feed = new FeedSubscriptionViewModel(feed, rss), CurrentGroups = feeds.Select(f => f.GroupName).Distinct().OrderBy(g => g) };
            */
            return new EditViewModel();
        }

        private class PocketAuthResult
        {
            [JsonPropertyName("access_token")]
            public string AccessToken{ get; set; }
            public string Username{ get; set; }
        }
    }
}
