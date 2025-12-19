// Initially created via https://aspire.dev/get-started/first-app/?lang=csharp

using Aspire.Hosting;
using Aspire.Hosting.RustFs;

var builder = DistributedApplication.CreateBuilder(args);

// https://github.com/konnta0/Aspire.Extensions/blob/main/src/Aspire.Hosting.RustFs/README.md
var rustfs = builder.AddRustFs("rustfs",
     accessKey: builder.AddParameter("rustfs-access-key", () => "rustfs-access-key"),
     secretKey: builder.AddParameter("rustfs-secret-key", () => "rustfs-secret-key"))
    .WithDataVolume();
var bucket = rustfs.AddBucket("my-bucket");

var apiService = builder.AddProject<Projects.AspireRustFS_ApiService>("apiservice")
    .WaitFor(rustfs)
    .WithEnvironment("RustFS__ServiceUrl", () =>
    {
        int port = rustfs.Resource.GetEndpoint("http").Port;
        string host = rustfs.Resource.GetEndpoint("http").Host;
        return $"http://{host}:{port}";
    })
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireRustFS_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
