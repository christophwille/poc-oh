using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctionsWorkflows.Workflows
{
    public class SayingHelloOrchestration
    {
        private readonly SampleService _theInjectedService;

        public SayingHelloOrchestration(SampleService theInjectedService)
        {
            _theInjectedService = theInjectedService;
        }

        [Function(nameof(SayingHelloOrchestration))]
        public async Task<List<string>> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(SayingHelloOrchestration));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            // Replace name and input with values relevant for your Durable Functions Activity
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHelloTask), "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHelloTask), "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHelloTask), "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }
    }
}
