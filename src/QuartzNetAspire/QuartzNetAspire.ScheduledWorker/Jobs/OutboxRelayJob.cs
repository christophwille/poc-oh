using System.Diagnostics;
using EasyNetQ;
using Quartz;
using QuartzNetAspire.Contracts;

namespace QuartzNetAspire.ScheduledWorker.Jobs;

/// <summary>
/// Continuous job: simulates a transactional-outbox relay that publishes pending rows to RabbitMQ.
/// Runs until the job's cancellation token fires (dashboard "Interrupt", scheduler shutdown).
/// ponytail: fake in-memory outbox (a counter). Upgrade path: poll an outbox table in the same database inside this
/// loop and delete rows after a confirmed publish.
/// </summary>
[DisallowConcurrentExecution]
public sealed class OutboxRelayJob(IBus bus, ILogger<OutboxRelayJob> logger) : IJob
{
    private static long lastOrderId;

    // Same name as the host's ApplicationName, which ServiceDefaults already subscribes with AddSource(...).
    private static readonly ActivitySource Activities = new("QuartzNetAspire.ScheduledWorker");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Outbox relay started (fire instance {FireInstanceId})", context.FireInstanceId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 11)), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // Each relayed order gets its own trace root. Without this every publish (and every delivery on the consumer
            // side) would nest under this job's single, never-ending Quartz.Job.Execute span.
            Activity.Current = null;
            using var activity = Activities.StartActivity("outbox.relay");

            var order = new OrderPlaced(Interlocked.Increment(ref lastOrderId), Random.Shared.Next(5, 500), DateTimeOffset.UtcNow);
            await bus.PubSub.PublishAsync(order, cancellationToken);
            activity?.SetTag("order.id", order.OrderId);
            logger.LogInformation("Relayed order {OrderId} ({Amount})", order.OrderId, order.Amount);
        }

        logger.LogInformation("Outbox relay stopped after order {OrderId}", lastOrderId);
    }
}
