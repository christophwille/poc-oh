using EasyNetQ;
using Quartz;
using QuartzNetAspire.Contracts;
using QuartzNetAspire.ScheduledWorker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEasyNetQ(EasyNetQConnection.FromAmqpUri(builder.Configuration.GetConnectionString("rabbitmq")!))
    .UseSystemTextJson();

// Quartz.Aspire: reads ConnectionStrings:quartz-scheduled, infers SQL Server, provisions the schema in Development,
// registers the scheduler health check and subscribes the "Quartz" activity source + meter on OpenTelemetry.
builder.AddQuartzPersistentStore("quartz-scheduled", settings =>
{
    settings.Clustered = true;
    // Explicit: the default only creates the schema when DOTNET_ENVIRONMENT=Development (set in launchSettings.json).
    settings.SchemaProvisioning = SchemaProvisioning.CreateIfMissing;
});

builder.AddQuartz(q =>
{
    q.ConfigureScheduler(options => options.InstanceName = "team-a-scheduled");

    // History rows go to the database so the central dashboard (store-attached) can show them cluster-wide.
    q.UsePersistentStore(store => store.UseExecutionHistory());

    // Also dial the central dashboard: the store-attached window sees the whole cluster but cannot reach into this
    // process (Interrupt, live logs are node-local), so each node additionally runs the agent.
    q.UseDashboardAgent(agent =>
    {
        var dashboardUrl = builder.Configuration["services:quartz-dashboard:http:0"]
            ?? throw new InvalidOperationException("quartz-dashboard endpoint not injected by Aspire");
        agent.Endpoint = new Uri(new Uri(dashboardUrl), "/quartz/agents");
        agent.Token = builder.Configuration["Dashboard:AgentToken"];
        agent.Target = $"team-a-{Environment.MachineName}".ToLowerInvariant(); // unique per process on the hub
        agent.IsJobTypeAllowed = typeName => typeName.StartsWith("QuartzNetAspire.ScheduledWorker.", StringComparison.Ordinal);
    });

    // Every log line inside a job carries quartz.job.name / quartz.trigger.name / fire instance id.
    q.AddJobLogScope();

    // --- scheduled tasks ---
    q.ScheduleJob<ReportJob>(trigger => trigger
        .WithIdentity("nightly-report")
        .WithDescription("Pretends to render the nightly report (every 2 minutes for the demo)")
        .WithCronSchedule("0 0/2 * ? * *"));

    q.ScheduleJob<CleanupJob>(trigger => trigger
        .WithIdentity("cleanup")
        .WithDescription("Pretends to purge stale rows; fails every 5th run on purpose")
        .WithSimpleSchedule(TimeSpan.FromSeconds(45)));   // repeats forever by default

    // --- continuous task: simulated transactional outbox relay ---
    // Durable so the dashboard can re-trigger it after an Interrupt. The StartNow trigger is re-added on every
    // startup, so a restarted process restarts the relay. Deliberately NOT RequestRecovery(): on a hard kill that
    // would queue a second run of a job that never ends (see HowItWasBuilt.md). The clustered persistent store
    // plus [DisallowConcurrentExecution] guarantees at most one relay per cluster.
    q.AddJob<OutboxRelayJob>(job => job
        .WithIdentity("outbox-relay")
        .WithDescription("Publishes OrderPlaced messages to RabbitMQ at random intervals until interrupted")
        .StoreDurably());

    q.AddTrigger(trigger => trigger
        .ForJob("outbox-relay")
        .WithIdentity("outbox-relay-start")
        .StartNow());
});

builder.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Build().Run();
