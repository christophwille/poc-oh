using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzFunc.ToTest
{
    public class SampleHttpFunctionNoDeps
    {
        private readonly ILogger<SampleHttpFunctionNoDeps> _logger;

        public SampleHttpFunctionNoDeps(ILogger<SampleHttpFunctionNoDeps> logger)
        {
            _logger = logger;
        }

        [Function("SampleHttpFunctionNoDeps")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
