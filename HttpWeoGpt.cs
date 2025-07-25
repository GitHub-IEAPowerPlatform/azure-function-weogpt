using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.Client;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
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

    [Function("HttpWeoGptAgentTool")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("🔧 Agent tool function triggered.");

        var tokenObject = await FetchTokenAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);

        if (tokenObject != null)
        {
            tokenObject["expires_in"] = 1800;
            var result = new ToolCallResult
            {
                Value = tokenObject.ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            };

            await response.WriteAsJsonAsync(result);
        }
        else
        {
            _logger.LogWarning("⚠️ Token response was null.");
            response.StatusCode = HttpStatusCode.InternalServerError;
        }

        return response;
    }

    private async Task<JObject?> FetchTokenAsync()
    {
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
                    return JObject.Parse(tokenResponse);
                }

                _logger.LogError($"⚠️ Token endpoint responded with status code: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"🔁 Retry {retryCount + 1}: {ex.Message}");
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
        }

        return null;
    }

    public class ToolCallResult
    {
        public required string Value { get; set; }
        public required string CorrelationId { get; set; }
    }
}
