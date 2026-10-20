using Quartz;

namespace QuartzNetAspire.ScheduledWorker.Jobs;

[DisallowConcurrentExecution]
public sealed class ReportJob(ILogger<ReportJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var rows = Random.Shared.Next(100, 5000);
        logger.LogInformation("Rendering report for {RowCount} rows", rows);
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 6)), cancellationToken);
        logger.LogInformation("Report rendered ({Size} KB)", rows / 7);
    }
}
