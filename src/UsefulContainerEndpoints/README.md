# Prompt

This is a boilerplate ASP.NET project. The intent is to add all useful container (Kubernetes) lifecycle and health endpoints in a standard and best practice way. This will always be a demo, not a production site. It is more important to document what can be done and why (and some "how" code samples) vs adding dummy implementation code.

# Result

Layout:

```
UsefulContainerEndpoints.slnx
global.json                                   # opts `dotnet test` into the Microsoft.Testing.Platform runner
README.md
UsefulContainerEndpoints/                     # the web app: Program.cs, launchSettings.json (8080 + 8081), .http requests
UsefulContainerEndpoints.IntegrationTests/    # NUnit + WebApplicationFactory tests, see below
```

Everything lives in [`Program.cs`](UsefulContainerEndpoints/Program.cs). Two ports: **8080** is the application, **8081** is the *management* port that carries probes, metrics and build info and is not exposed through a Service/Ingress. Locally `launchSettings.json` binds both; in the container that is `ASPNETCORE_HTTP_PORTS=8080;8081` (set by the csproj for `PublishContainer`, and again in the Deployment YAML). Requests are in [`UsefulContainerEndpoints.http`](UsefulContainerEndpoints/UsefulContainerEndpoints.http).

| Endpoint | Port | Answers | Runs | Status codes |
|---|---|---|---|---|
| `/healthz/live` | 8081 | liveness probe | **no checks** (`Predicate = _ => false`) | 200 if Kestrel answers |
| `/healthz/ready` | 8081 | readiness probe | `ApplicationLifecycle`, `container resources` (Degraded only) (+ your real dependencies) | 200 Healthy/Degraded, 503 Unhealthy |
| `/healthz/startup` | 8081 | startup probe | `Manual` (flipped by `WarmupService` after `Warmup:DelaySeconds`, default 10) | 503 until warm-up done, then 200 |
| `/metrics` | 8081 | Prometheus scrape | OTel ASP.NET Core + runtime instrumentation | 200 `text/plain; version=0.0.4` (OpenMetrics only if the scraper asks for it via `Accept`) |
| `/info` | 8081 | humans / rollout debugging | assembly informational version, environment, pod name, start time, uptime | 200 |
| `/weatherforecast` | 8080 (any) | the actual app | – | 200 |

Packages: `Microsoft.Extensions.Diagnostics.HealthChecks.Common` (lifecycle + manual checks), `Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Runtime`, `OpenTelemetry.Exporter.Prometheus.AspNetCore` (beta, see below). `Microsoft.Extensions.Diagnostics.HealthChecks` itself does **not** need a `PackageReference` in a web project - it ships in the ASP.NET Core shared framework (the SDK flags an explicit reference as NU1510).

## The three probes - what and why

Kubernetes has three probes and they answer three different questions. Mixing them up is the #1 mistake.

### Startup: "is initialization finished?"

```csharp
builder.Services.AddHealthChecks().AddManualHealthCheck(tags: ["startup"]);
builder.Services.AddHostedService<WarmupService>();

internal sealed class WarmupService(IManualHealthCheck<WarmupService> health) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await WarmCachesRunMigrationsLoadModels(ct);   // the demo just sleeps Warmup:DelaySeconds (default 10)
        health.ReportHealthy();                        // initial state is Unhealthy("Initial state")
    }
}

mgmt.MapHealthChecks("/healthz/startup", new HealthCheckOptions { Predicate = r => r.Tags.Contains("startup") });
```

Until the startup probe succeeds once, kubelet runs *neither* liveness nor readiness. That is the whole point: a slow starter (JIT, big EF model, cache pre-load, ML model download) is not killed by a liveness probe that fires too early, and you don't need to bloat `initialDelaySeconds` on the other probes. The startup budget is `failureThreshold * periodSeconds` (e.g. `30 * 5s` = 2.5 min); after that the container is restarted. The manual health check (`IManualHealthCheck<T>`, `ReportHealthy()` / `ReportUnhealthy(reason)`) is the standard way to hand a "done" signal from a hosted service to the health system.

(`Warmup:DelaySeconds` is deliberately an integer: binding a `TimeSpan` from configuration makes `Warmup__Delay=5` mean five *days* - the string `"5"` parses as `5.00:00:00` - which shows up as "startup probe fails forever" with no log hint.)

### Liveness: "is the process wedged?"

```csharp
mgmt.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = _ => false });
```

Zero checks. If Kestrel can serve a request the process is not dead-locked/starved, and that is *all* liveness should assert, because the only remedy k8s has is **restarting the container**. Never put a database, message broker or downstream API into liveness: if the DB goes down, every replica fails liveness, every replica gets restarted in a loop, and you have turned an outage of one dependency into a restart storm of your whole fleet - and restarts do not fix a dead DB. Also don't add the resource-utilization check here: high memory near the limit is a reason to shed traffic (readiness), not to restart.

### Readiness: "should traffic be routed to this pod?"

```csharp
builder.Services.AddHealthChecks()
    .AddApplicationLifecycleHealthCheck(tags: ["ready"])
    .AddResourceUtilizationHealthCheck(o =>
    {
        o.UseObservableResourceMonitoringInstruments = true;   // [Experimental] EXTEXP0007, see below
        o.CpuThresholds    = new ResourceUsageThresholds { DegradedUtilizationPercentage = 80 };   // no Unhealthy threshold
        o.MemoryThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 80 };
    }, tags: ["ready"]);

mgmt.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready"), ResponseWriter = WriteJson });
```

Failing readiness only removes the pod from the Service endpoints; nothing is restarted. That makes it the right place for everything that means "I can't serve right now":

* **`ApplicationLifecycle`** - `Unhealthy("Not Started")` before `IHostApplicationLifetime.ApplicationStarted`, `Unhealthy("Stopping")` after `ApplicationStopping`. The *Not Started* half is the valuable one: a pod is not ready until every hosted service has started (Kestrel starts last in minimal hosting, so this is mostly a guard for custom `IServer` setups and for the JSON body). The *Stopping* half is honest but nearly invisible in practice - see [Graceful shutdown](#graceful-shutdown-sigterm).
* **`container resources`** (`ResourceUtilization`) - CPU and memory as a percentage of the container's cgroup **limits** (`container.cpu.limit.utilization`, `container.memory.limit.utilization` from `Microsoft.Extensions.Diagnostics.ResourceMonitoring`), so "80 %" means 80 % of `resources.limits`, not of the node. Three things you must know:
  * It is **Degraded-only** here (no `UnhealthyUtilizationPercentage`). A load-based 503 in readiness is a documented cascading-failure pattern: a traffic spike pushes pod A over the threshold, three failed probes later it is removed, pod B now takes twice the load and crosses the threshold too, and a fleet that could have served everyone with worse latency serves nobody. HPA works in minutes, `periodSeconds * failureThreshold` in seconds. Degraded still returns 200, but the JSON body / metrics show it - which is what you want from this check. If you must fail on it, fail on **memory** only, with a threshold well above where GC would have kicked in.
  * `UseObservableResourceMonitoringInstruments = true` is `[Experimental]` (build error EXTEXP0007 without a `#pragma warning disable`). With the default `false` the check uses `IResourceMonitor.GetUtilization()`, whose CPU percentage is relative to the cgroup **request** (`cpu.weight` → `GuaranteedCpuUnits`), not the limit: with `requests: 250m, limits: 1` a pod using 0.24 cores reports ~96 %. Memory happens to be limit-relative in both modes because k8s doesn't set `memory.min`. Either accept the experimental flag (this demo) or document that "CPU %" means "% of request".
  * Windows/Linux only: `AddResourceMonitoring()` throws at host start on macOS ([dotnet/extensions#5962](https://github.com/dotnet/extensions/issues/5962)), hence the `if (!OperatingSystem.IsMacOS())` guard in `Program.cs`. Outside a container the "limits" are the host's (Windows: process share of the machine).
* **Real dependencies** - not in this demo, see [Not implemented](#not-implemented-but-available). Be selective: only dependencies without which *every* request fails. A dependency used by 5 % of endpoints should degrade those endpoints, not pull the pod.

Default status mapping of `MapHealthChecks`: Healthy 200, Degraded 200, Unhealthy 503; override with `HealthCheckOptions.ResultStatusCodes` if needed. Responses carry `Cache-Control: no-store` by default (`AllowCachingResponses = false`) - keep it that way, kubelet must never get a cached answer.

The plain-text body (`Healthy`) is enough for Kubernetes - it looks only at the status code. The small `WriteJson` writer in `Program.cs` exists for humans (`kubectl exec ... curl localhost:8081/healthz/ready` tells you *which* check failed). If you want the richer standard format that dashboards understand, use `AspNetCore.HealthChecks.UI.Client`'s `UIResponseWriter.WriteHealthCheckUIResponse` instead of rolling your own.

Naming: `/healthz/{live,ready,startup}` mirrors the k8s probe names. The other Microsoft-blessed convention is Aspire's `ServiceDefaults`: `/health` (all checks) and `/alive` (checks tagged `live`), both on the app port and only in Development unless you opt in. Same mechanism (`MapHealthChecks` + tag predicates), different names and no dedicated port - the reasons for the choices here are in [Management port](#management-port).

## Graceful shutdown (SIGTERM)

```csharp
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("SIGTERM received ..."));
```

```yaml
terminationGracePeriodSeconds: 30
env: [{ name: shutdownTimeoutSeconds, value: "20" }]     # HostOptions.ShutdownTimeout, see below
lifecycle: { preStop: { sleep: { seconds: 5 } } }
```

What happens when a pod is deleted (rollout, scale-down, node drain):

1. Pod goes to `Terminating`. The endpoint controller flags it as *terminating / not ready* in the EndpointSlice **immediately and regardless of your readiness probe** - it is the deletion timestamp that does this, not a 503. What takes time is kube-proxy / the ingress controller / a cloud LB *acting* on that change (seconds).
2. kubelet runs the `preStop` hook, then sends **SIGTERM** to PID 1.
3. The .NET generic host fires `ApplicationStopping` and then stops hosted services in reverse registration order. In minimal hosting (`WebApplication`) the Kestrel host service is registered *last* by `Build()`, so it stops **first**: Kestrel unbinds 8080 and 8081, then waits for in-flight requests to finish. Only after that do your own hosted services get their `stoppingToken`.
4. The host waits up to `HostOptions.ShutdownTimeout` (default 30 s) for all of that, then exits.
5. If the process is still alive after `terminationGracePeriodSeconds` (default 30 s) kubelet sends **SIGKILL**.

Consequences:

* **The readiness flip does not drain traffic.** Between step 3's `ApplicationStopping` and Kestrel unbinding lie milliseconds; a probe arriving in that window sees `Unhealthy("Stopping")`, every later one sees *connection refused* - and k8s stopped caring at step 1 anyway. What protects late requests is **step 2**: the `preStop` sleep gives step 1's propagation time to finish while the app still listens. Keep it (5 s is the usual number); do not drop it because "readiness flips".
* If you cannot use `preStop` (no k8s, or a platform without hooks), do the sleep in-process: an `IHostedLifecycleService` whose `StoppingAsync` awaits ~5 s runs *before any* `StopAsync`, i.e. with Kestrel still bound and readiness already 503 - that is the only place where the `Stopping` state is observable for long enough to matter. Or register your draining hosted service via `builder.Host.ConfigureContainer(...)`/after `Build()` so it lands *after* the Kestrel host service and stops *before* it.
* **`ShutdownTimeout` + `preStop` must be smaller than `terminationGracePeriodSeconds`**, otherwise you drain nicely and get SIGKILLed anyway. `HostOptions` binds `shutdownTimeoutSeconds` (also `startupTimeoutSeconds`, `servicesStartConcurrently`, `servicesStopConcurrently`) from configuration, so set it as an env var right next to `terminationGracePeriodSeconds` in the manifest instead of hard-coding `Configure<HostOptions>` in `Program.cs` - the code version silently wins over the config knob.
* SIGTERM must reach the process. `dotnet publish -t:PublishContainer` writes an exec-form `ENTRYPOINT ["dotnet", "App.dll"]` so the app *is* PID 1; a hand-written `ENTRYPOINT ["sh", "-c", ...]` (or `ENTRYPOINT dotnet App.dll` shell form) puts a shell at PID 1 that does not forward SIGTERM - you always hit the 30 s SIGKILL path.

## Management port

```csharp
var mgmt = app.MapGroup("/").WithMetadata(new ManagementEndpoint());   // marker metadata
app.Use((ctx, next) =>                                                  // runs after routing, before the endpoint
{
    if (ctx.GetEndpoint()?.Metadata.GetMetadata<ManagementEndpoint>() is not null && ctx.Connection.LocalPort != 8081)
        ctx.SetEndpoint(null);                                          // -> 404
    return next(ctx);
});
mgmt.MapHealthChecks(...); mgmt.MapPrometheusScrapingEndpoint(); mgmt.MapGet("/info", ...);
```

Probes and metrics are unauthenticated and leak internals (versions, memory, which dependency is down). Putting them on a second Kestrel port that the Service/Ingress does not forward is the simplest way to keep them internal - kubelet probes reach the pod IP directly, so it doesn't matter that no Service exposes 8081, and Prometheus scrapes pod IPs as well.

**Not `RequireHost("*:8081")`.** That is the routing feature the docs show for "filter by port", but it matches the client-supplied `Host` header, not the listener - `curl -H 'Host: x:8081' http://svc/metrics` through the Service, or through an ingress that forwards the original host, opens every management endpoint on the app port; and `kubectl port-forward pod 9000:8081` sends `Host: localhost:9000` and gets a 404 from the real management listener. The Microsoft health-checks doc itself notes that RequireHost "is subject to potential spoofing" and points at `HttpContext.Connection.LocalPort`. The middleware above decides on the socket the connection arrived on; a `MapWhen(c => c.Connection.LocalPort == 8081, ...)` branch would be the alternative, but inside `WebApplication` a branch's `UseRouting()` reuses the *global* endpoint table, so the management endpoints would still match on 8080 - hence metadata + one middleware.

Ports come from `ASPNETCORE_HTTP_PORTS=8080;8081`. 8080 has been the default of the official images since .NET 8 (they moved off 80 *so that* the app can run as non-root, see [Dockerfile / image](#not-implemented-but-available)); the base image only knows about 8080, so the csproj adds `<ContainerEnvironmentVariable Include="ASPNETCORE_HTTP_PORTS" Value="8080;8081" />` and `<ContainerPort Include="8081" />` for `PublishContainer`, and the Deployment repeats the env var so the manifest is self-describing.

Alternatives if you can't spare a port: `RequireAuthorization()` on the health group (kubelet then needs a token → awkward), or Aspire-style "probes on the app port, but only mapped in Development" (fine for internal services behind a mesh, not for anything internet-facing). A **NetworkPolicy** is not an alternative but the natural companion: once the split is a real listener, an L4 rule "8081 only from kubelet/Prometheus namespaces" is exactly what policies are good at.

## Metrics

```csharp
builder.Services.AddOpenTelemetry().WithMetrics(m => m
    .AddAspNetCoreInstrumentation()   // http.server.request.duration etc. (built-in System.Diagnostics.Metrics)
    .AddRuntimeInstrumentation()      // GC, thread pool, JIT, exceptions
    .AddPrometheusExporter());
mgmt.MapPrometheusScrapingEndpoint();  // /metrics
```

`/metrics` is the pull-model endpoint for Prometheus (`prometheus.io/scrape: "true"`, `prometheus.io/port: "8081"` annotations or a `ServiceMonitor`/`PodMonitor`). Caveat: `OpenTelemetry.Exporter.Prometheus.AspNetCore` has been *beta* since 2022 and the OTel .NET team has said it will stay that way until the spec settles - fine for a demo, but the GA path is `OpenTelemetry.Exporter.OpenTelemetryProtocol` **pushing** to an OTel Collector (or `dotnet-monitor` as sidecar exposing `/metrics` without touching the app), which is also what Aspire's `ServiceDefaults` do. Health-check results can be published as metrics/logs too via `AddTelemetryHealthCheckPublisher()` from `HealthChecks.Common` (uses the `IHealthCheckPublisher` mechanism, i.e. periodic evaluation independent of probes).

## `/info`

```csharp
var started = Process.GetCurrentProcess().StartTime.ToUniversalTime();   // once, not per request
mgmt.MapGet("/info", () => new
{
    version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion, // not GetEntryAssembly(): under a test host that is the test runner
    environment = app.Environment.EnvironmentName,
    machine = Environment.MachineName,   // == pod name
    started, uptime = DateTime.UtcNow - started,
});
```

Not a Kubernetes concept, but the endpoint you'll be glad to have during a rollout: `kubectl exec <pod> -- curl -s localhost:8081/info` answers "which build is *this* pod really running?". The SDK stamps `InformationalVersion` as `<Version>+<git sha>` out of the box for git repos (`SourceRevisionId`, `IncludeSourceRevisionInInformationalVersion`) - that's the `1.0.0+9231093...` you see here; the `GitShaEmbed` sample in this repo shows the `MSBuildGitHash` alternative for embedding the SHA as an assembly attribute.

## Deployment YAML

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: usefulcontainerendpoints
spec:
  replicas: 2
  template:
    metadata:
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "8081"
        prometheus.io/path: "/metrics"
    spec:
      terminationGracePeriodSeconds: 30        # > preStop sleep (5) + shutdownTimeoutSeconds (20)
      containers:
        - name: app
          image: usefulcontainerendpoints:1.0.0
          env:
            - name: ASPNETCORE_HTTP_PORTS
              value: "8080;8081"
            - name: shutdownTimeoutSeconds      # HostOptions.ShutdownTimeout, keep next to terminationGracePeriodSeconds
              value: "20"
            - name: ASPNETCORE_FORWARDEDHEADERS_ENABLED   # X-Forwarded-For/-Proto from the ingress, see "Not implemented"
              value: "true"
          ports:
            - name: http
              containerPort: 8080
            - name: management
              containerPort: 8081
          resources:                            # limits are what ResourceUtilization measures against (with UseObservableResourceMonitoringInstruments)
            requests: { cpu: 250m, memory: 256Mi }
            limits:   { cpu: "1",  memory: 512Mi }
          startupProbe:
            httpGet: { path: /healthz/startup, port: management }
            periodSeconds: 5
            failureThreshold: 30                # 150s startup budget, no initialDelaySeconds needed anywhere
          livenessProbe:
            httpGet: { path: /healthz/live, port: management }
            periodSeconds: 10
            timeoutSeconds: 2
            failureThreshold: 3
          readinessProbe:
            httpGet: { path: /healthz/ready, port: management }
            periodSeconds: 5
            timeoutSeconds: 2
            failureThreshold: 3
            successThreshold: 1
          lifecycle:
            preStop:
              sleep: { seconds: 5 }             # GA since k8s 1.30 (gated 1.29); older: exec: {command: [sleep, "5"]}
---
apiVersion: v1
kind: Service
metadata:
  name: usefulcontainerendpoints
spec:
  selector: { app: usefulcontainerendpoints }
  ports:
    - name: http
      port: 80
      targetPort: http                          # 8081 deliberately not exposed
```

## Not implemented, but available

**A real dependency in readiness.** Either the Xabaril family (`AspNetCore.HealthChecks.SqlServer`, `.NpgSql`, `.Redis`, `.RabbitMQ`, `.Uris`, ~60 packages), e.g.

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default")!, tags: ["ready"], timeout: TimeSpan.FromSeconds(2));
```

or a custom `IHealthCheck` when you want to control exactly what "reachable" means (and always give it a timeout - a hanging check makes the *probe* time out, which k8s counts as a failure):

```csharp
sealed class QueueHealthCheck(IQueueClient q) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext ctx, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            var depth = await q.GetDepthAsync(cts.Token);
            return depth > 10_000
                ? HealthCheckResult.Degraded($"backlog {depth}", data: new Dictionary<string, object> { ["depth"] = depth })
                : HealthCheckResult.Healthy();
        }
        catch (Exception ex) { return HealthCheckResult.Unhealthy("queue unreachable", ex); }
    }
}
builder.Services.AddHealthChecks().AddCheck<QueueHealthCheck>("queue", tags: ["ready"]);
```

**In-process drain delay** (when `preStop` is not available) - the .NET 8+ `IHostedLifecycleService` gives you a hook that runs before *any* hosted service is stopped, i.e. while Kestrel is still listening and readiness already answers 503:

```csharp
sealed class DrainDelay : IHostedLifecycleService
{
    public Task StoppingAsync(CancellationToken ct) => Task.Delay(TimeSpan.FromSeconds(5), ct);   // endpoint removal propagates meanwhile
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;   // the other four members: Task.CompletedTask
    ...
}
```

**Forwarded headers** - the companion of "TLS terminates at the ingress": without it `Request.Scheme` is `http` and `RemoteIpAddress` is the ingress pod, so absolute URLs, OpenAPI server URLs, OIDC `redirect_uri` and per-IP rate limits are wrong. `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (in the YAML above) turns on `X-Forwarded-For`/`X-Forwarded-Proto` handling for the immediate proxy; for a fixed ingress CIDR prefer `UseForwardedHeaders` with `KnownNetworks`/`KnownProxies` set explicitly (the env-var version clears both, i.e. trusts whoever is in front of you - fine behind a cluster ingress, not on a node port).

**Structured logs** - `Logging:Console:FormatterName=json` (env `Logging__Console__FormatterName=json`) makes the console logger emit one JSON object per line, which is what Fluent Bit / Loki / Datadog collectors want; no package needed.

**TCP probes instead of HTTP** - `Microsoft.Extensions.Diagnostics.Probes` (`services.AddKubernetesProbes(o => { o.LivenessProbe.TcpPort = 2305; ... })`) opens/closes raw TCP listeners based on the same health checks, for `tcpSocket:` probes on services that have no HTTP surface (workers). Still `-preview` as of 10.9, hence not used here.

**Health checks in a Worker Service** (no Kestrel): same registrations, but expose them via the Probes package above, `dotnet-monitor`, or the publisher (`AddTelemetryHealthCheckPublisher`) - the `MapHealthChecks` middleware needs ASP.NET Core.

**Dockerfile / image** - not needed; `dotnet publish -t:PublishContainer` (see `WebApiPublishAsContainer` in this repo) produces an image that runs as the non-root `app` user (`ContainerUser`, default since .NET 8) with an exec-form `ENTRYPOINT`, so the app is PID 1 and gets SIGTERM. Note that these are properties of *PublishContainer*, not of the base image: `mcr.microsoft.com/dotnet/aspnet:10.0` itself runs as **root** and has no `ENTRYPOINT` - a hand-written `FROM ... aspnet:10.0` Dockerfile needs its own `USER app` and exec-form `ENTRYPOINT` (and `EXPOSE 8081`, plus `ASPNETCORE_HTTP_PORTS=8080;8081`). The port moved from 80 to 8080 in .NET 8 precisely so that non-root works.

**Removed from the template**: `app.UseHttpsRedirection()`. In a cluster TLS terminates at the ingress/mesh; the pod speaks plain HTTP. With only HTTP ports configured the middleware can't determine an HTTPS port and silently no-ops (one warning at startup) - dead code at best. If someone later sets `ASPNETCORE_HTTPS_PORT` (or an https URL) it turns into a 307 to `https://pod:<port>/...`; kubelet follows same-host redirects, nothing listens with TLS there → probe fails → pod never becomes ready / gets restarted. Drop it in containers, or at minimum keep the management group out of its reach.

## Integration tests

`UsefulContainerEndpoints.IntegrationTests` (NUnit + Microsoft.Testing.Platform, `dotnet test` from this folder - `global.json` opts into the MTP runner) hosts the real `Program.cs` in-memory via `WebApplicationFactory<Program>` and asserts the contract Kubernetes relies on:

* liveness is 200/`Healthy`/`Cache-Control: no-store`; readiness is 200 (Healthy *or* Degraded - the resource check depends on the machine running the tests) and lists `ApplicationLifecycle` + `container resources`; `/metrics` and `/info` have the expected shape.
* the management endpoints answer on **8081 only** - and a spoofed `Host: localhost:8081` on the 8080 client still gets 404. `TestServer` has no sockets, so `Connection.LocalPort` is never set; the factory adds an `IStartupFilter` that copies a test-only `X-Test-LocalPort` header (set per `HttpClient`) into `LocalPort` before routing. Deliberately not derived from the `Host` header, otherwise the spoofing test would prove nothing. (.NET 10's `WebApplicationFactory.UseKestrel()` would give real sockets at the price of fixed ports.)
* startup is 503 (`Manual` / `Initial state`) until `WarmupService` finishes, then 200. The delay is pinned per test host via `Warmup:DelaySeconds` (a day for "before", zero for "after"; zero completes inside `StartAsync`, so no polling), so nothing races a timer.
* SIGTERM: `IHostApplicationLifetime.StopApplication()` is the same code path as SIGTERM. Readiness flips to 503 `Stopping`, liveness stays 200, the app still serves. Because the minimal-hosting factory really runs `app.Run()`, the host would shut down immediately - the test registers a hosted service whose `StopAsync` blocks (simulated in-flight work) to hold the host open long enough to observe the state, and releases it in a `finally` (otherwise a failed assertion hangs `Dispose` for `ShutdownTimeout` and hides the message). This proves the health-check state machine, not Kestrel's drain - `TestServer` has nothing to unbind.

Only `Program.cs` had to change for testability: `public partial class Program;` at the end (the documented hook for `WebApplicationFactory`) and the `Warmup:DelaySeconds` knob.

## Pitfalls checklist

* Dependency checks in **liveness** → restart storm on a dependency outage. Liveness = no checks.
* CPU/memory as an **Unhealthy** readiness condition → load spike pulls pods one after the other, zero endpoints. Degraded-only, or a separate reporting endpoint.
* `RequireHost("*:8081")` as the "management port" → spoofable via `Host`, breaks `port-forward`. Decide on `Connection.LocalPort`.
* `UseHttpsRedirection` in a container → no-op noise at best, probes redirected to a TLS port nobody listens on at worst.
* Big `initialDelaySeconds` on liveness/readiness instead of a **startup probe** → slow first requests or, worse, killed during JIT/warm-up.
* `timeoutSeconds` (default **1 s**) ≥ `periodSeconds`, or a health check without its own timeout → sporadic false failures under load.
* No `resources.limits` → `ResourceUtilization` percentages measure against the node, i.e. mean nothing. And without `UseObservableResourceMonitoringInstruments`, CPU % is relative to the *request*.
* Management port exposed via Service/Ingress → unauthenticated internals on the internet.
* Dropping the `preStop` sleep because "readiness flips on SIGTERM" → Kestrel unbinds within milliseconds of SIGTERM; late requests get connection refused.
* `HostOptions.ShutdownTimeout` (+ preStop) ≥ `terminationGracePeriodSeconds` → SIGKILL mid-drain, dropped requests.
* Shell-wrapped `ENTRYPOINT` → SIGTERM never reaches the app, always the 30 s SIGKILL path.
* Cached probe responses (a proxy in between, or `AllowCachingResponses = true`) → k8s acts on stale state.
* `Degraded` treated as failure by a custom `ResultStatusCodes` → pods pulled from rotation for a warning-level condition.
* Binding a duration knob as `TimeSpan` from env → `Foo__Delay=5` is five days.

## Links

* https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks - `MapHealthChecks`, `HealthCheckOptions`, publishers, filtering by tags; "Filter by port" and its RequireHost spoofing note
* https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer - forwarded headers behind an ingress
* https://kubernetes.io/docs/concepts/configuration/liveness-readiness-startup-probes/ and https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/
* https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/#pod-termination - the SIGTERM sequence; "terminating endpoints always have their ready status as false"
* https://learn.microsoft.com/dotnet/core/extensions/generic-host#host-shutdown and https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.ihostedlifecycleservice - stop order, `StoppingAsync`
* https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.HealthChecks.Common - lifecycle / manual / telemetry publisher
* https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization and https://github.com/dotnet/extensions/issues/5962 (macOS)
* https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Probes - TCP probes (preview)
* https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks - dependency checks + UI response writer
* https://learn.microsoft.com/dotnet/core/diagnostics/built-in-metrics-aspnetcore - what `AddAspNetCoreInstrumentation` exposes
* https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore - the "why still beta" discussion lives in its issues
* https://learn.microsoft.com/dotnet/core/docker/container-images and https://learn.microsoft.com/dotnet/core/docker/publish-as-container - port 8080, `ASPNETCORE_HTTP_PORTS`, `ContainerUser`/`ContainerPort`
* https://careersatdoordash.com/blog/how-to-handle-kubernetes-health-checks/ - the cascading-failure story behind "Degraded-only"; https://github.com/kubernetes/kubernetes/issues/115917 - k8s has no Envoy-style panic threshold for readiness

### Verified against (2026-08-18)

Claims in this README that go beyond the docs were checked against the actual sources rather than taken from memory:

* CPU % basis of the resource check (limit vs. request, `UseObservableResourceMonitoringInstruments`): [ResourceUtilizationHealthCheck.cs](https://github.com/dotnet/extensions/blob/main/src/Libraries/Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization/ResourceUtilizationHealthCheck.cs) (dotnet/extensions 10.9.0)
* `shutdownTimeoutSeconds` / `startupTimeoutSeconds` / `servicesStartConcurrently` / `servicesStopConcurrently` config keys: [HostOptions.cs](https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Hosting/src/HostOptions.cs) (`Initialize`)
* macOS start-up failure of `AddResourceMonitoring()`: [dotnet/extensions#5962](https://github.com/dotnet/extensions/issues/5962) (open)
* `RequireHost` spoofing / `Connection.LocalPort`: [health-checks doc, "Filter by port"](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks#filter-by-port), plus a live Kestrel run of this app: `curl -H 'Host: localhost:8081' :8080/healthz/live` → 404, `curl -H 'Host: localhost:9000' :8081/healthz/live` → 200
* Terminating endpoints are not-ready regardless of the probe: [Pod termination](https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/#pod-termination); Kestrel stops first: `WebApplicationBuilder.Build()` appends the `GenericWebHostService` descriptor last (`src/DefaultBuilder/src/WebApplicationBuilder.cs` in dotnet/aspnetcore)
* Cascading readiness failure: [DoorDash post-mortem](https://careersatdoordash.com/blog/how-to-handle-kubernetes-health-checks/), [kubernetes#115917](https://github.com/kubernetes/kubernetes/issues/115917)
* Base image runs as root, non-root comes from `PublishContainer`: [Securing containers with rootless](https://devblogs.microsoft.com/dotnet/securing-containers-with-rootless/) ("if you don't do anything ... your image will continue to run as root"), [.NET 8 port change 80 → 8080](https://learn.microsoft.com/dotnet/core/compatibility/containers/8.0/aspnet-port), [publish-as-container](https://learn.microsoft.com/dotnet/core/docker/publish-as-container) (`ContainerUser`, `ContainerPort`)
* `/metrics` content type (`text/plain; version=0.0.4`), preStop `sleep` GA in 1.30: observed on this app / [k8s 1.30 release notes](https://kubernetes.io/blog/2024/04/17/kubernetes-v1-30-release/)
