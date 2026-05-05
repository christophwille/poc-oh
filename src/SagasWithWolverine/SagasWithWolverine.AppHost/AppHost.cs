var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver").AddDatabase("sagastate");

// var rabbitUser = builder.AddParameter("RabbitUser"); // would be config value "Parameters:RabbitUser"

var rabbitMq = builder.AddRabbitMQ("rabbitmq",
        userName: builder.AddParameter("username", "admin", secret: true),
        password: builder.AddParameter("password", "admin", secret: true))
    .WithManagementPlugin();

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
