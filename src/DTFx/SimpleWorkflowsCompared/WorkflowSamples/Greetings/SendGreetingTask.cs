// Copyright (c) Jacob Viau. All rights reserved.
// Licensed under the APACHE 2.0. See LICENSE file in the project root for full license information.

using DurableTask.Core;

namespace DurableTask.Samples.Greetings;

public sealed class SendGreetingTask : AsyncTaskActivity<string, string>
{
    protected override async Task<string> ExecuteAsync(TaskContext context, string user)
    {
        string message;
        if (!string.IsNullOrWhiteSpace(user) && user.Equals("TimedOut"))
        {
            message = "GetUser Timed out!!!";
        }
        else
        {
            await Task.Delay(5 * 1000);
            message = "Greeting sent to " + user;
        }

        return message;
    }
}
