using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeoGpt.Function;

public class HttpWeoGptAgentTool
{
    private readonly ILogger<HttpWeoGptAgentTool> _logger;
    private static readonly HttpClient httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public HttpWeoGptAgentTool(ILogger<HttpWeoGptAgentTool> logger)
    {
        _logger = logger;
    }
     //Azure Function URL: https://vscode-iea-weogpt-hmfbg4bha7fzc2bx.francecentral-01.azurewebsites.net/api/HttpWeoGptAgentTool?
    [Function("HttpWeoGptAgentTool")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        _logger.LogInformation("🔧 HttpWeoGptAgentTool function triggered.");

        var response = req.CreateResponse();

        try
        {
            var tokenObject = await FetchTokenAsync();

            if (tokenObject != null)
            {
                tokenObject.ExpiresIn = 1800;

                var json = JsonSerializer.Serialize(tokenObject, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync(json);
            }
            else
            {
                _logger.LogWarning("⚠️ Token response was null.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Token fetch failed.");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"❌ HTTP error: {ex.Message}");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync("HTTP request failed.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError($"⏱️ Request timeout: {ex.Message}");
            response.StatusCode = HttpStatusCode.RequestTimeout;
            await response.WriteStringAsync("Request timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"💥 Unexpected error: {ex.Message}");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync("Unexpected error occurred.");
        }

        return response;
    }

    private async Task<DirectLineTokenResponse?> FetchTokenAsync()
    {
        //Copilot Studio Project Infos:
        //Solution Name: WEO-Agent-Gen Solution, 
        //Agent Name: WEO GPT 2024 - Generative AI 
        //UAT Token  Endpoint: "https://1c30aed158d94c96a7128ab2269f56.d2.environment.api.powerplatform.com/powervirtualagents/botsbyschema/iea_weoGpt2024Advanced/directline/token?api-version=2022-03-01-preview";
        //PROD Token Endpoint: "https://bc1cc217c7794ddb9683cfc02bb30a.9d.environment.api.powerplatform.com/powervirtualagents/botsbyschema/iea_weoGpt2024Advanced/directline/token?api-version=2022-03-01-preview"
        
        string tokenEndPoint = "https://1c30aed158d94c96a7128ab2269f56.d2.environment.api.powerplatform.com/powervirtualagents/botsbyschema/iea_weoGpt2024Advanced/directline/token?api-version=2022-03-01-preview";

        int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var response = await httpClient.GetAsync(tokenEndPoint);
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await response.Content.ReadAsStringAsync();
                    var tokenObject = JsonSerializer.Deserialize<DirectLineTokenResponse>(tokenResponse);

                    if (tokenObject != null)
                    {
                        return tokenObject;
                    }

                    _logger.LogError("❗ Token response was empty.");
                    return null;
                }

                _logger.LogError($"⚠️ Token endpoint responded with status code: {response.StatusCode}");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"🔁 Retry {retryCount + 1}: HTTP error - {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError($"🔁 Retry {retryCount + 1}: Timeout - {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"🔁 Retry {retryCount + 1}: Unexpected error - {ex.Message}");
            }

            retryCount++;
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))); // Exponential backoff
        }

        return null;
    }
}
