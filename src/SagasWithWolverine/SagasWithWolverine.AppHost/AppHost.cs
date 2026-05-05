var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver").AddDatabase("sagastate");

var rabbitMq = builder.AddRabbitMQ("rabbitmq");

var apiService = builder.AddProject<Projects.SagasWithWolverine_ApiService>("apiservice")
    .WithReference(sqlServer)
    .WithReference(rabbitMq)
    .WithHttpHealthCheck("/health")
    .WaitFor(sqlServer)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.SagasWithWolverine_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
