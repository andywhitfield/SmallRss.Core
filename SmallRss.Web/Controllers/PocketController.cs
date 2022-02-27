using System.Net.Http;
using System.Text;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public PocketController(ILogger<PocketController> logger,
            IUserAccountRepository userAccountRepository,
            IArticleRepository articleRepository,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _articleRepository = articleRepository;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<object> PostAsync([FromForm] PocketViewModel model)
        {
            var userAccount = await _userAccountRepository.FindOrCreateAsync(User);
            if (!userAccount.HasPocketAccessToken)
                return new { saved = false, reason = "Your account is not connected to Pocket." };

            var article = await _articleRepository.GetByIdAsync(model.ArticleId.GetValueOrDefault());
            if (article == null)
                return new { saved = false, reason = "Could not find article with id " + model.ArticleId };

            var requestJson = JsonSerializer.Serialize(new
            {
                consumer_key = ManageController.PocketConsumerKey,
                access_token = userAccount.PocketAccessToken,
                url = HttpUtility.UrlPathEncode(article.Url),
                title = HttpUtility.UrlEncode(article.Heading ?? string.Empty)
            });

            _logger.LogInformation($"Saving article [{article.Id}:{article.Url}:{article.Heading}] to pocket");

            using var pocketClient = _httpClientFactory.CreateClient(Startup.PocketHttpClient);
            using var response = await pocketClient.PostAsync("add", new StringContent(requestJson, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error response attempting to save to pocket: {response.StatusCode}");
                return new { saved = false };
            }

            var result = await response.Content.ReadAsStringAsync();
            if (!result.TryParseJson(out AddResponse? addResult, _logger))
                return new { saved = false };

            _logger.LogInformation($"Successfully saved article [{article.Id}:{article.Url}:{article.Heading}] to pocket");
            // TODO: handle response and return appropriate json response to client
            return new { saved = true };
        }

        private class AddResponse
        {
            public int Status { get; set; }
        }
    }
}
