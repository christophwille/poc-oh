using EasyNetQ;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Quartz;
using Quartz.Diagnostics;
using QuartzNetAspire.Contracts;
using QuartzNetAspire.QueueWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEasyNetQ(EasyNetQConnection.FromAmqpUri(builder.Configuration.GetConnectionString("rabbitmq")!))
    .UseSystemTextJson();

builder.AddQuartz(q =>
{
    q.ConfigureScheduler(options => options.InstanceName = "team-b-queue");

    // In-memory store: this worker has no database. Every replica runs its own scheduler and its own listener.
    q.UseInMemoryStore();
    q.AddJobLogScope();

    // Dial the central dashboard: outbound SignalR connection, no inbound port, no shared database.
    q.UseDashboardAgent(agent =>
    {
        var dashboardUrl = builder.Configuration["services:quartz-dashboard:http:0"]
            ?? throw new InvalidOperationException("quartz-dashboard endpoint not injected by Aspire");
        agent.Endpoint = new Uri(new Uri(dashboardUrl), "/quartz/agents");
        agent.Token = builder.Configuration["Dashboard:AgentToken"];
        // Targets must be unique per process on the hub; on Kubernetes the pod name alone is unique.
        agent.Target = $"team-b-{Environment.MachineName}".ToLowerInvariant();
        agent.IsJobTypeAllowed = typeName => typeName.StartsWith("QuartzNetAspire.QueueWorker.", StringComparison.Ordinal);
    });

    // Continuous task: durable so the dashboard can Trigger it again after an Interrupt.
    q.AddJob<OrderListenerJob>(job => job
        .WithIdentity("order-listener")
        .WithDescription("Subscribes to OrderPlaced via EasyNetQ and processes messages until interrupted")
        .StoreDurably());

    q.AddTrigger(trigger => trigger
        .ForJob("order-listener")
        .WithIdentity("order-listener-start")
        .StartNow());

    q.AddQuartzHealthChecks();
});

builder.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// Without Quartz.Aspire the Quartz activity source and meter have to be subscribed by hand.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(QuartzInstrumentation.ActivitySourceName))
    .WithMetrics(metrics => metrics.AddMeter(QuartzInstrumentation.MeterName));

builder.Build().Run();
