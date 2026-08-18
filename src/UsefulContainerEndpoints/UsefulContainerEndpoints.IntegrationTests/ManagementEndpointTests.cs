using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using NUnit.Framework;

namespace UsefulContainerEndpoints.IntegrationTests;

/// <summary>Steady-state behaviour of the management endpoints (one shared host, warm-up never completes).</summary>
[TestFixture]
public sealed class ManagementEndpointTests
{
    private AppFactory _factory = null!;
    private HttpClient _mgmt = null!;
    private HttpClient _app = null!;

    [OneTimeSetUp]
    public void StartHost()
    {
        _factory = new AppFactory();
        _mgmt = _factory.Management;
        _app = _factory.App;
    }

    [OneTimeTearDown]
    public void StopHost()
    {
        _mgmt.Dispose();
        _app.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Liveness_is_200_plain_text_and_not_cacheable()
    {
        var response = await _mgmt.GetAsync("/healthz/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
        response.Headers.CacheControl!.NoStore.Should().BeTrue("kubelet must never act on a cached probe result");
    }

    [Test]
    public async Task Readiness_is_200_and_lists_lifecycle_and_resource_checks()
    {
        var response = await _mgmt.GetAsync("/healthz/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // "container resources" is Degraded-only and depends on the test machine's load, so don't pin the overall status to Healthy.
        body.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded");
        var checks = body.GetProperty("checks").EnumerateArray().ToDictionary(c => c.GetProperty("name").GetString()!, c => c.GetProperty("status").GetString());
        checks["ApplicationLifecycle"].Should().Be("Healthy");
        if (!OperatingSystem.IsMacOS()) // see Program.cs
        {
            checks["container resources"].Should().NotBe("Unhealthy", "utilization must never pull a pod from rotation");
        }
    }

    [Test]
    public async Task Startup_is_503_while_warmup_is_running()
    {
        var response = await _mgmt.GetAsync("/healthz/startup");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var manual = body.GetProperty("checks").EnumerateArray().Single();
        manual.GetProperty("name").GetString().Should().Be("Manual");
        manual.GetProperty("status").GetString().Should().Be("Unhealthy");
        manual.GetProperty("description").GetString().Should().Be("Initial state");
    }

    [Test]
    public async Task Metrics_serves_prometheus_text_with_runtime_and_http_metrics()
    {
        await _app.GetAsync("/weatherforecast"); // produce at least one http.server.request.duration sample

        var response = await _mgmt.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("dotnet_").And.Contain("http_server_request_duration");
    }

    [Test]
    public async Task Info_reports_environment_and_pod_identity()
    {
        var body = await _mgmt.GetFromJsonAsync<JsonElement>("/info");

        body.GetProperty("environment").GetString().Should().Be("Development");
        body.GetProperty("machine").GetString().Should().Be(Environment.MachineName);
        body.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("uptime").GetString().Should().NotBeNullOrEmpty();
    }

    [TestCase("/healthz/live")]
    [TestCase("/healthz/ready")]
    [TestCase("/healthz/startup")]
    [TestCase("/metrics")]
    [TestCase("/info")]
    public async Task Management_endpoints_are_not_reachable_on_the_app_port(string path)
    {
        var response = await _app.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Management_port_check_cannot_be_spoofed_via_the_Host_header()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/healthz/live");
        request.Headers.Host = "localhost:8081"; // what RequireHost("*:8081") would have accepted

        var response = await _app.SendAsync(request); // ...but the connection arrived on the app port

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Application_endpoint_still_works_on_the_app_port()
    {
        var response = await _app.GetAsync("/weatherforecast");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength().Should().Be(5);
    }
}
