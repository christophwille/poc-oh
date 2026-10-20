using Quartz;

namespace QuartzNetAspire.ScheduledWorker.Jobs;

[DisallowConcurrentExecution]
public sealed class CleanupJob(ILogger<CleanupJob> logger) : IJob
{
    private static int runs;

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var run = Interlocked.Increment(ref runs);
        logger.LogInformation("Cleanup run {Run} started", run);
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 4)), cancellationToken);

        if (run % 5 == 0)
        {
            // Lets the dashboard history and the quartz.job.execution.duration{quartz.job.result=failed} metric show failures.
            throw new InvalidOperationException($"Simulated failure: lock timeout while purging batch {run}");
        }

        logger.LogInformation("Cleanup run {Run} purged {Rows} rows", run, Random.Shared.Next(0, 300));
    }
}
