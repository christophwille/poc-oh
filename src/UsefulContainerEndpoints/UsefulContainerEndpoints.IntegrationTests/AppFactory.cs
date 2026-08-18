using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UsefulContainerEndpoints.IntegrationTests;

/// <summary>
/// In-memory host (TestServer) of the real Program.cs. The warm-up delay is pinned per fixture so the
/// startup-probe tests are deterministic instead of racing a 10s timer.
/// </summary>
internal sealed class AppFactory(int? warmupSeconds = null, bool holdShutdown = false) : WebApplicationFactory<Program>
{
    // Default: never finish warm-up, so "before warm-up" assertions can't flake.
    private readonly int _warmupSeconds = warmupSeconds ?? (int)TimeSpan.FromDays(1).TotalSeconds;

    /// <summary>
    /// Only with holdShutdown: simulated in-flight work - host shutdown blocks in this service's StopAsync until Release().
    /// </summary>
    public DrainGate Drain { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Warmup:DelaySeconds", _warmupSeconds.ToString());
        builder.ConfigureTestServices(s =>
        {
            s.AddSingleton<IStartupFilter, FakeLocalPort>();
            if (holdShutdown)
            {
                // Registered last => stopped first (reverse order), so the server keeps serving while the gate is closed.
                s.AddHostedService(_ => Drain);
            }
        });
    }

    public sealed class DrainGate : IHostedService
    {
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Release() => _released.TrySetResult();
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct) => _released.Task.WaitAsync(ct);
    }

    // The app decides "management or not" on Connection.LocalPort (the listener), which TestServer never sets - no sockets involved.
    // This runs before routing and copies a test-only header into LocalPort; deliberately not derived from the Host header,
    // so the tests can prove that a spoofed Host does not open the management endpoints.
    private sealed class FakeLocalPort : IStartupFilter
    {
        public const string Header = "X-Test-LocalPort";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((ctx, n) =>
            {
                ctx.Connection.LocalPort = int.TryParse(ctx.Request.Headers[Header], out var port) ? port : 0;
                return n(ctx);
            });
            next(app);
        };
    }

    public HttpClient Management => ClientOn(8081);
    public HttpClient App => ClientOn(8080);

    private HttpClient ClientOn(int port)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://localhost:{port}") });
        client.DefaultRequestHeaders.Add(FakeLocalPort.Header, port.ToString());
        return client;
    }
}
