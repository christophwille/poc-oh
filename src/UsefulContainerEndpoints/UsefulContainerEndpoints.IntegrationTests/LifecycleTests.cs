using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace UsefulContainerEndpoints.IntegrationTests;

/// <summary>State transitions: each test owns its host because it mutates lifecycle state.</summary>
[TestFixture]
public sealed class LifecycleTests
{
    [Test]
    public async Task Startup_becomes_200_once_warmup_has_finished()
    {
        // Delay 0 completes synchronously inside BackgroundService.StartAsync, i.e. before the host is up - no polling needed.
        using var factory = new AppFactory(warmupSeconds: 0);
        using var mgmt = factory.Management;

        var response = await mgmt.GetAsync("/healthz/startup");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Test]
    public async Task Sigterm_fails_readiness_but_keeps_liveness_and_the_app_serving()
    {
        using var factory = new AppFactory(holdShutdown: true);
        try
        {
            using var mgmt = factory.Management;
            using var app = factory.App;
            (await mgmt.GetAsync("/healthz/ready")).StatusCode.Should().Be(HttpStatusCode.OK, "precondition: ready before SIGTERM");

            // Same path as SIGTERM: ApplicationStopping fires, then the host stops services in reverse order.
            // The DrainGate (simulated in-flight work) blocks that, so we can observe the drain window.
            factory.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

            var ready = await mgmt.GetAsync("/healthz/ready");
            ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "readiness must report the shutdown");
            var body = await ready.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("checks").EnumerateArray().Single(c => c.GetProperty("name").GetString() == "ApplicationLifecycle")
                .GetProperty("description").GetString().Should().Be("Stopping");

            (await mgmt.GetAsync("/healthz/live")).StatusCode.Should().Be(HttpStatusCode.OK, "draining is not a reason to restart");
            (await app.GetAsync("/weatherforecast")).StatusCode.Should().Be(HttpStatusCode.OK, "in-flight/late requests are still served during the drain");
        }
        finally
        {
            factory.Drain.Release(); // always let the host finish, otherwise Dispose blocks for ShutdownTimeout and hides the assertion
        }
    }
}
