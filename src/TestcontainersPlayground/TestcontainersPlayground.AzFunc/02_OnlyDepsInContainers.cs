using AzFunc.ToTest;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;

namespace TestcontainersPlayground;

public class FunctionAppFixture : IAsyncLifetime
{
    private readonly IHost _host;

    public IServiceProvider ServiceProvider => _host.Services;

    /*
    [0]: "--host"
    [1]: "127.0.0.1"
    [2]: "--port"
    [3]: "65107"
    [4]: "--workerId"
    [5]: "7a22e78c-32f2-4e6e-8acd-c01a41c52647"
    [6]: "--requestId"
    [7]: "87a6eb21-3625-41a6-bd31-4f3f7d491565"
    [8]: "--grpcMaxMessageLength"
    [9]: "2147483647"
    [10]: "--functions-uri"
    [11]: "http://127.0.0.1:65107/"

    [12]: "--functions-worker-id"
    [13]: "7a22e78c-32f2-4e6e-8acd-c01a41c52647"
    [14]: "--functions-request-id"
    [15]: "87a6eb21-3625-41a6-bd31-4f3f7d491565"
    [16]: "--functions-grpc-max-message-length"
    [17]: "2147483647"  */
    public FunctionAppFixture()
    {
        // TODO: It is being ignored...
        var builder = FunctionsApplication.CreateBuilder([
            "--host", "127.0.0.1",
            "--port", "65107",
            "--functions-uri", "http://127.0.0.1:65107/"
            ]);
        builder.ConfigureFunctionsWebApplication();

        builder.Configuration
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // https://github.com/mawax/azure-functions-integration-testing/blob/main//ExampleFunctionApp/ExampleFunctionApp.Integration.Tests/HostBuilderExtensions.cs#L20
        // Otherwise, this exception on _host.StartAsync:
        // Collection fixture type 'TestcontainersPlayground.FunctionAppFixture' threw in InitializeAsync
        // ----System.InvalidOperationException : Configuration is missing the 'HostEndpoint' information.Please ensure an entry with the key 'Functions:Worker:HostEndpoint' is present in your configuration. 
        var hostedService = builder.Services.First(descriptor => descriptor.ImplementationType?.Name == "WorkerHostedService");
        builder.Services.Remove(hostedService);

        _host = builder.Build();

        // HERE: Azurite and MsSql Testcontainers
    }

    public async ValueTask InitializeAsync()
    {
        await _host.StartAsync();
    }
    public async ValueTask DisposeAsync()
    {
        _host?.Dispose();
    }
}
[CollectionDefinition("FunctionApp")]
public class FunctionAppCollection : ICollectionFixture<FunctionAppFixture>
{
}

[Collection("FunctionApp")]
public class Approach2Tests : IClassFixture<FunctionAppFixture>
{
    private readonly FunctionAppFixture _fixture;

    public Approach2Tests(FunctionAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Request_ReturnsResponseWithServerTimingHeader()
    {
        // One thing for sure: we are IHostBuilder Isolated v2 (that is a difference to all other older approaches)
        // Is this maybe related to MTS being no longer IsTestProject and output being .exe with in-built Main?

        var sut = ActivatorUtilities.CreateInstance<SampleHttpFunctionNoDeps>(_fixture.ServiceProvider);

        // sut.Run(new HttpRequest())
    }
}