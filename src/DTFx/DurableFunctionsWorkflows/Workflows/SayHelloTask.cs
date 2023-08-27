using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctionsWorkflows.Workflows
{
    public class SayHelloTask
    {
        [Function(nameof(SayHelloTask))]
        public string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHelloTask");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }
    }
}
