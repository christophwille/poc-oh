// Copyright (c) Jacob Viau. All rights reserved.
// Licensed under the APACHE 2.0. See LICENSE file in the project root for full license information.

using DurableTask.Core;
using WorkflowSamples;

namespace DurableTask.Samples.Greetings;

public class GreetingsOrchestration : TaskOrchestration<string, string>
{
    private readonly SampleService _theInjectedService;

    public GreetingsOrchestration(SampleService theInjectedService)
    {
        _theInjectedService = theInjectedService;
    }

    public override async Task<string> RunTask(OrchestrationContext context, string input)
    {
        string user = await context.ScheduleTask<string>(typeof(GetUserTask));
        string greeting = await context.ScheduleTask<string>(typeof(SendGreetingTask), user);
        return greeting;
    }
}
