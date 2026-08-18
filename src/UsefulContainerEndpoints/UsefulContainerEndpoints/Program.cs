using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using OpenTelemetry.Metrics;

// See README.md for the what/why. Ports: 8080 = app, 8081 = management (probes, metrics, info).

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var health = builder.Services.AddHealthChecks()
    // readiness: Unhealthy("Not Started") before ApplicationStarted, Unhealthy("Stopping") after ApplicationStopping (SIGTERM).
    // The "Not Started" half is the useful one; during SIGTERM k8s has already pulled the endpoint (see README).
    .AddApplicationLifecycleHealthCheck(tags: ["ready"])
    // startup: Unhealthy until WarmupService says otherwise.
    .AddManualHealthCheck(tags: ["startup"]);
// readiness, Degraded only: CPU/memory relative to the container *limits*. Deliberately no Unhealthy threshold -
// a load-based 503 pulls pods exactly when the fleet needs them (cascading failure). Visible in the JSON body / metrics.
if (!OperatingSystem.IsMacOS()) // dotnet/extensions#5962: AddResourceMonitoring throws at host start on macOS
{
    health.AddResourceUtilizationHealthCheck(o =>
    {
#pragma warning disable EXTEXP0007 // experimental API; the alternative (default false) measures CPU against the cgroup *request*, not the limit
        o.UseObservableResourceMonitoringInstruments = true; // container.cpu.limit.utilization / container.memory.limit.utilization
#pragma warning restore EXTEXP0007
        o.CpuThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 80 };
        o.MemoryThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 80 };
    }, tags: ["ready"]);
}
builder.Services.AddHostedService<WarmupService>();

// SIGTERM: HostOptions.ShutdownTimeout (default 30 s) is set via config key "shutdownTimeoutSeconds" next to
// terminationGracePeriodSeconds in the Deployment YAML, not hard-coded here.

builder.Services.AddOpenTelemetry().WithMetrics(m => m
    .AddAspNetCoreInstrumentation()
    .AddRuntimeInstrumentation()
    .AddPrometheusExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Deliberately no app.UseHttpsRedirection(): TLS terminates at the ingress, kubelet probes plain HTTP.

app.Lifetime.ApplicationStopping.Register(() =>
    app.Logger.LogInformation("SIGTERM received: readiness now reports Unhealthy, draining in-flight requests"));

// ---- management endpoints: only served on the 8081 *listener* (do not expose that port via Service/Ingress) ----
// Checked on the connection, not via RequireHost("*:8081"): the Host header is client-supplied and spoofable.
var mgmt = app.MapGroup("/").WithMetadata(new ManagementEndpoint());
app.Use((ctx, next) =>
{
    if (ctx.GetEndpoint()?.Metadata.GetMetadata<ManagementEndpoint>() is not null && ctx.Connection.LocalPort != ManagementEndpoint.Port)
    {
        ctx.SetEndpoint(null); // routing matched, but on the app port -> fall through to 404
    }
    return next(ctx);
});

// liveness: "is the process wedged?" - runs zero checks; if Kestrel answers, the process is alive. Never test dependencies here.
mgmt.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = _ => false });
// readiness: "should traffic be routed here?"
mgmt.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready"), ResponseWriter = WriteJson });
// startup: "is initialization done?" - k8s runs live/ready only after this passed once.
mgmt.MapHealthChecks("/healthz/startup", new HealthCheckOptions { Predicate = r => r.Tags.Contains("startup"), ResponseWriter = WriteJson });
// Prometheus scrape target
mgmt.MapPrometheusScrapingEndpoint(); // /metrics
// build/pod identity for rollout debugging ("which version is this pod actually running?")
var started = Process.GetCurrentProcess().StartTime.ToUniversalTime();
mgmt.MapGet("/info", () => new
{
    version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    environment = app.Environment.EnvironmentName,
    machine = Environment.MachineName, // == pod name in k8s
    started,
    uptime = DateTime.UtcNow - started,
});

// ---- the actual application ----
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

// k8s only looks at the status code; the JSON body is for humans debugging "which check failed?".
static Task WriteJson(HttpContext ctx, HealthReport report) => ctx.Response.WriteAsJsonAsync(new
{
    status = report.Status.ToString(),
    totalDuration = report.TotalDuration,
    checks = report.Entries.Select(e => new
    {
        name = e.Key,
        status = e.Value.Status.ToString(),
        description = e.Value.Description,
        duration = e.Value.Duration,
        data = e.Value.Data.Count > 0 ? e.Value.Data : null,
    }),
});

/// <summary>Endpoint metadata marking "management port only"; the middleware in Program enforces it on Connection.LocalPort.</summary>
internal sealed class ManagementEndpoint
{
    public const int Port = 8081;
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Simulates cache warm-up / migrations / preloading. Until ReportHealthy() the startup probe answers 503,
// so k8s neither runs liveness/readiness yet nor restarts a slowly starting pod.
internal sealed class WarmupService(IManualHealthCheck<WarmupService> health, IConfiguration config, ILogger<WarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Warmup:DelaySeconds is a knob for demos/tests, not something a real service would have.
        await Task.Delay(TimeSpan.FromSeconds(config.GetValue<int?>("Warmup:DelaySeconds") ?? 10), stoppingToken);
        health.ReportHealthy();
        logger.LogInformation("Warm-up done, startup probe now Healthy");
    }
}

// Makes the implicit Program class visible to WebApplicationFactory<Program> in the integration tests.
public partial class Program;
