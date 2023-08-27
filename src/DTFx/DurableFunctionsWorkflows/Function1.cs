using DurableFunctionsWorkflows.Workflows;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsWorkflows
{
    public static class Function1
    {
        [Function("Function1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(SayingHelloOrchestration));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [Function("Function1_HttpRaiseEvent")]
        public static async Task HttpRaiseEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            var eventData = await req.ReadFromJsonAsync<TheEventIWasWaitingOn>();

            await client.RaiseEventAsync(eventData.OrchestrationInstanceId,
                SayingHelloOrchestration.TheEventIWasWaitingOn,
                eventPayload: eventData.ABoolIWasExpecting);
        }
    }

    public record TheEventIWasWaitingOn(string OrchestrationInstanceId, bool ABoolIWasExpecting);
}
