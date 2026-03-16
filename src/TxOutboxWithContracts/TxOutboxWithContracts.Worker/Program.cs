using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text.Json;
using TxOutboxWithContracts.Data;
using TxOutboxWithContracts.FakeBus;
using TxOutboxWithContracts.Messages;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddDbContext<SqlServerBloggingContext>();
        services.AddTransient<IPubSub, FakePubSub>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

var context = services.GetRequiredService<SqlServerBloggingContext>();
var pubsub = services.GetRequiredService<IPubSub>();

var contractsAssembly = Assembly.Load(typeof(BlogCreated).Assembly.GetName());

// We'd need additionally: cancellation, retries, error handling, logging, exponential backoff, dead-lettering

var pendingMessages = await context.Outbox
  .Where(m => m.ProcessedOnUtc == null)
  .OrderBy(m => m.CreatedOnUtc)
  .Take(100)
  .ToListAsync();

foreach (var message in pendingMessages)
{
    try
    {
        var eventType = contractsAssembly.GetType(message.Type);
        var @event = JsonSerializer.Deserialize(message.Data, eventType);

        // Note: you could wrap this with an envelope OR have a base class with idempotency key & deduplication id
        //       or eg add the Outbox.Id so the consumer could write information back to the Outbox (eg failed to process)
        await pubsub.PublishAsync(@event);

        message.ProcessedOnUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        // Handle error (e.g. log it, retries, dead-lettering, etc)
    }
}