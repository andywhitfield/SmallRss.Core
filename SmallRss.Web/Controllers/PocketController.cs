using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallRss.Data;
using SmallRss.Web.Models;

namespace SmallRss.Web.Controllers
{
    [Authorize, ApiController, Route("api/[controller]")]
    public class PocketController : ControllerBase
    {
        private readonly ILogger<PocketController> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IArticleRepository _articleRepository;

        public PocketController(ILogger<PocketController> logger,
            IUserAccountRepository userAccountRepository,
            IArticleRepository articleRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _articleRepository = articleRepository;
        }

        [HttpPost]
        public async Task<ActionResult<object>> PostAsync([FromForm]PocketViewModel model)
        {
            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            if (!userAccount.HasPocketAccessToken)
                return new { saved = false, reason = "Your account is not connected to Pocket." };

            var article = await _articleRepository.GetByIdAsync(model.ArticleId.GetValueOrDefault());
            if (article == null)
                return new { saved = false, reason = "Could not find article with id " + model.ArticleId };

            var requestJson = JsonSerializer.Serialize(new {
                consumer_key = ManageController.PocketConsumerKey,
                access_token = userAccount.PocketAccessToken,
                url = HttpUtility.UrlPathEncode(article.Url),
                title = HttpUtility.UrlEncode(article.Heading ?? string.Empty)
            });

            var webClient = new WebClient();
            webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json; charset=UTF-8");
            webClient.Headers.Add("X-Accept", "application/json");
            var result = webClient.UploadString("https://getpocket.com/v3/add", requestJson);
            if (!result.TryParseJson(out AddResponse addResult, _logger))
                return new { saved = false };
            // TODO: handle response and return appropriate json response to client
            return new { saved = true };
        }

        private class AddResponse
        {
            public string Status { get; set; }
        }
    }
}
