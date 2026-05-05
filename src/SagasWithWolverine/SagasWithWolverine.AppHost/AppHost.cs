var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.SagasWithWolverine_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.SagasWithWolverine_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
