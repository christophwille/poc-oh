using EasyNetQ;
using Quartz;
using QuartzNetAspire.Contracts;

namespace QuartzNetAspire.QueueWorker;

/// <summary>
/// Continuous job: holds an EasyNetQ subscription open until the job's cancellation token fires
/// (dashboard "Interrupt" or scheduler shutdown). Each message is fake work.
/// </summary>
[DisallowConcurrentExecution]
public sealed class OrderListenerJob(IBus bus, ILogger<OrderListenerJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var processed = 0;

        await using var subscription = await bus.PubSub.SubscribeAsync<OrderPlaced>(
            subscriptionId: "queue-worker",
            onMessage: async (order, ct) =>
            {
                logger.LogInformation("Processing order {OrderId} ({Amount}) placed at {PlacedAt}", order.OrderId, order.Amount, order.PlacedAt);
                await Task.Delay(Random.Shared.Next(200, 800), ct);
                Interlocked.Increment(ref processed);
            },
            configure: _ => { },
            cancellationToken: cancellationToken);

        logger.LogInformation("Order listener subscribed to queue {Queue}", subscription.Queue.Name);

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Interrupt or shutdown: fall through and let the subscription dispose.
        }

        logger.LogInformation("Order listener stopped after {Count} messages", processed);
    }
}
