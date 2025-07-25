using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace WeoGpt.Function;

public class HttpWeoGpt
{
    private readonly ILogger<HttpWeoGpt> _logger;

    public HttpWeoGpt(ILogger<HttpWeoGpt> logger)
    {
        _logger = logger;
    }

    [Function("HttpWeoGpt")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}