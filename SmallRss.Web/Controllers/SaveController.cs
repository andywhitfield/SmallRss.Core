using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmallRss.Data;
using SmallRss.Models;
using SmallRss.Web.Models;

namespace SmallRss.Web.Controllers;

[Authorize, ApiController, Route("api/[controller]")]
public class SaveController(ILogger<SaveController> logger,
    IUserAccountRepository userAccountRepository,
    IArticleRepository articleRepository,
    IHttpClientFactory httpClientFactory,
    IOptionsSnapshot<RaindropOptions> raindropOptions)
    : ControllerBase
{
    [HttpPost]
    public async Task<object> PostAsync([FromForm] PocketViewModel model)
    {
        var article = await articleRepository.GetByIdAsync(model.ArticleId.GetValueOrDefault());
        if (article == null)
            return new { saved = false, reason = "Could not find article with id " + model.ArticleId };

        var userAccount = await userAccountRepository.GetAsync(User);

        if (userAccount.HasPocketAccessToken)
            return await SaveToPocketAsync(userAccount, article);
        
        if (userAccount.HasRaindropRefreshToken)
            return await SaveToRaindropAsync(userAccount, article);

        return new { saved = false, reason = "Your account is not connected to Pocket or Raindrop.io" };
    }

    private async Task<object> SaveToPocketAsync(UserAccount userAccount, Article article)
    {
        var requestJson = JsonSerializer.Serialize(new
        {
            consumer_key = ManageController.PocketConsumerKey,
            access_token = userAccount.PocketAccessToken,
            url = HttpUtility.UrlPathEncode(article.Url),
            title = HttpUtility.UrlEncode(article.Heading ?? string.Empty)
        });

        logger.LogInformation($"Saving article [{article.Id}:{article.Url}:{article.Heading}] to pocket");

        using var pocketClient = httpClientFactory.CreateClient(Startup.PocketHttpClient);
        using var response = await pocketClient.PostAsync("add", new StringContent(requestJson, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Error response attempting to save to pocket: {response.StatusCode}");
            return new { saved = false };
        }

        var result = await response.Content.ReadAsStringAsync();
        if (!result.TryParseJson(out PocketAddResponse? addResult, logger))
            return new { saved = false };

        logger.LogInformation($"Successfully saved article [{article.Id}:{article.Url}:{article.Heading}] to pocket");
        // TODO: handle response and return appropriate json response to client
        return new { saved = true };
    }

    private async Task<object> SaveToRaindropAsync(UserAccount userAccount, Article article)
    {
        using var raindropClient = httpClientFactory.CreateClient(Startup.RaindropHttpClient);

        var accessToken = await GetRaindropAccessTokenAsync(raindropClient, userAccount);
        if (accessToken == null)
            return new { saved = false };

        raindropClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var requestJson = JsonSerializer.Serialize(new
        {
            pleaseParse = new {},
            link = article.Url
        });
        logger.LogInformation($"Saving article {article.Id}:{article.Url} {requestJson} to raindrop.io");

        using var response = await raindropClient.PostAsync("/rest/v1/raindrop", new StringContent(requestJson, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Error response attempting to save to raindrop.io: {response.StatusCode}");
            return new { saved = false };
        }

        var result = await response.Content.ReadAsStringAsync();
        if (!result.TryParseJson(out RaindropAddResponse? addResult, logger) || !(addResult?.Result ?? false))
        {
            logger.LogError($"Could not save article to raindrop.io: {result}");
            return new { saved = false };
        }

        logger.LogInformation($"Successfully saved article [{article.Id}:{article.Url}:{article.Heading}] to raindrop.io");
        return new { saved = true };
    }

    private async Task<string?> GetRaindropAccessTokenAsync(HttpClient raindropClient, UserAccount userAccount)
    {
        var requestJson = JsonSerializer.Serialize(new { client_id = raindropOptions.Value.ClientId, client_secret = raindropOptions.Value.ClientSecret, grant_type = "refresh_token", refresh_token = userAccount.RaindropRefreshToken });
        using var response = await raindropClient.PostAsync("https://raindrop.io/oauth/access_token",
            new StringContent(requestJson, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Error getting access token from refresh token: {response.StatusCode}");
            return null;
        }

        var result = await response.Content.ReadAsStringAsync();
        if (!result.TryParseJson(out RaindropTokenResult? authResult, logger) || authResult?.AccessToken == null)
        {
            logger.LogError($"Could not get access token from refresh token: {result}");
            return null;
        }
        
        logger.LogInformation($"Got access token result: result={result}");
        return authResult?.AccessToken;
    }

    private class PocketAddResponse
    {
        public int Status { get; set; }
    }

    private class RaindropTokenResult
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private class RaindropAddResponse
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }
    }
}
