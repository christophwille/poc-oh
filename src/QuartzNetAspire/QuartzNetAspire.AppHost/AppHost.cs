var builder = DistributedApplication.CreateBuilder(args);

// One SQL Server container; Team A's Quartz job store lives in its own database.
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);
var quartzDb = sql.AddDatabase("quartz-scheduled");

var rabbit = builder.AddRabbitMQ("rabbitmq",
        userName: builder.AddParameter("rabbit-user", "admin", secret: true),
        password: builder.AddParameter("rabbit-password", "admin", secret: true))
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

// Shared secret between the dashboard's agent hub and the workers that dial in.
var agentToken = builder.AddParameter("dashboard-agent-token", "demo-agent-token", secret: true);

// Central Quartz dashboard: attaches to Team A's store, accepts agents from everyone.
var dashboard = builder.AddProject<Projects.QuartzNetAspire_Dashboard>("quartz-dashboard", launchProfileName: "http")
    .WithReference(quartzDb)
    .WithEnvironment("Dashboard__AgentToken", agentToken)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(quartzDb); // the attached store is rediscovered every minute, so it may come up before the schema exists

// Team A: scheduled jobs + outbox relay, SQL Server persistent clustered store; also runs the dashboard agent.
var scheduledWorker = builder.AddProject<Projects.QuartzNetAspire_ScheduledWorker>("scheduled-worker")
    .WithReference(quartzDb)
    .WithReference(rabbit)
    .WithReference(dashboard)
    .WithEnvironment("Dashboard__AgentToken", agentToken)
    .WaitFor(quartzDb)
    .WaitFor(rabbit);

// Team B: queue listener, in-memory store, reports to the dashboard through the agent.
builder.AddProject<Projects.QuartzNetAspire_QueueWorker>("queue-worker")
    .WithReference(rabbit)
    .WithReference(dashboard)
    .WithEnvironment("Dashboard__AgentToken", agentToken)
    .WaitFor(rabbit)
    .WaitFor(dashboard);

builder.Build().Run();
