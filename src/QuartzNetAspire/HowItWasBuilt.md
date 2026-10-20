# How It Was Built

A record of the requirements, the challenge to the original proposal, the design decisions and the pitfalls behind this Quartz.NET 4 / Aspire sample. The main intent is to **evaluate one central Quartz dashboard across workers that are deployed by different teams on their own schedules** — the fake jobs only exist to light that dashboard up.

## Requirements

| # | Requirement |
|---|-------------|
| 1 | Scheduled tasks running at specific times (Quartz.NET 4.x) |
| 2 | Continuous tasks that listen to RabbitMQ queues (EasyNetQ) |
| 3 | A continuous task that publishes to RabbitMQ at random intervals, simulating a transactional-outbox relay, so the listener has a real end-to-end flow |
| 4 | Workers are deployed by **separate teams on separate schedules** (today Windows services / scheduled tasks, soon containers on Kubernetes). Merging them into one deployment is not an option. All teams are on .NET 10 or newer. |
| 5 | **One central monitoring dashboard** based on `Quartz.Dashboard` |
| 6 | Quartz OpenTelemetry integration wired into the Aspire dashboard |
| 7 | Everything hosted in Aspire for easy testing: one dashboard site, one database dependency (SQL Server), a few demo jobs. Kubernetes is not simulated. |
| 8 | Validate that it starts, runs, and reports to both dashboards; write a demo guide and this document |

Decisions made during planning (with the user): the queue listener is modelled as **one long-running Quartz job**, and the dashboard demonstrates **both** store-attached and agent connectivity.

## Challenge of the proposal

1. **Quartz 4 is `net10.0`-only** (no `netstandard2.0`, no .NET Framework, no `quartz.config`). Teams are on .NET 10+, so this is a prerequisite, not a blocker. A worker on Quartz 3.x cannot appear in this dashboard.
2. **A queue consumer is not a scheduler concern.** As a long-running Quartz job it does show up (Currently Executing, Interrupt, Trigger now, history), which is what the central dashboard needs. The catch is the store: with a *clustered persistent* store, only one node runs the job, so consumers cannot scale out. Team B therefore uses the **in-memory store**: every Kubernetes replica runs its own scheduler and its own listener, each visible through its own agent target. The **outbox relay** is the mirror image — it *must* run on exactly one node — so it lives in Team A's **clustered persistent store**, where `[DisallowConcurrentExecution]` plus the cluster lock give exactly-one semantics.
3. **Sharing one set of Quartz tables couples teams.** `SCHED_NAME` lets many schedulers share tables, but a mandatory schema migration (4.0 had one) would then force all teams to move together. Recommendation: database per team on the shared server; the dashboard can `AttachStore` each one. The sample attaches one.
4. **The store-attached window cannot reach into a process.** It reads and writes the database, so pause/resume/trigger/delete work cluster-wide, but **Interrupt, Live Logs, Start/Standby/Shutdown are node-local** and are not offered on the window (the dashboard says so on its front page). For a demo whose highlight is "stop the listener from the dashboard", every node also needs an agent (or the HTTP API). Team A runs both: the window for the cluster-wide picture, the agent for control.
5. **The dashboard has no authentication of its own** and can schedule any job type by name. The sample uses `AllowAnonymous()` plus `IsJobTypeAllowed` whitelists on the dashboard, the HTTP API and both agents. Production needs `RequireAuthorization()` behind the company IdP, and the agent token in a secret store.
6. **Execution history** is cluster-wide only with `UseExecutionHistory()` on both the workers' store and the attached store (extra `QRTZ_EXECUTION_HISTORY` / `QRTZ_MISFIRE_HISTORY` / `QRTZ_JOB_STATUS` tables). Agent history is in-memory per worker and lost on restart.
7. **`Quartz.Aspire` is a client integration, not an AppHost resource.** The AppHost only declares SQL Server and RabbitMQ and passes connection strings; Quartz runs inside the project resources.
8. **Quartz does not start containers.** On Kubernetes every Quartz-hosted worker is an always-on Deployment, even for a single 02:00 job. The README has a Quartz-vs-CronJob trade-off table and the launcher pattern for teams that cannot justify an idle pod.

## Solution design

```mermaid
graph LR
    subgraph AppHost["QuartzNetAspire.AppHost (Aspire 13.6)"]
        SQL[("sql / quartz-scheduled<br/>SQL Server 2022")]
        MQ[("rabbitmq<br/>+ management UI")]
        A["scheduled-worker (Team A)<br/>Quartz.Aspire · clustered ADO store<br/>nightly-report · cleanup · outbox-relay"]
        B["queue-worker (Team B)<br/>in-memory store<br/>order-listener"]
        D["quartz-dashboard<br/>Quartz.Dashboard + HTTP API"]
    end
    A -->|QRTZ_* tables, history| SQL
    D -.->|AttachStore window| SQL
    A -->|agent: SignalR, bearer token| D
    B -->|agent: SignalR, bearer token| D
    A -->|OrderPlaced via EasyNetQ| MQ
    MQ -->|subscription| B
    A & B & D -->|OTLP| Aspire((Aspire dashboard))
```

| Project | Role | Key packages |
|---|---|---|
| `QuartzNetAspire.AppHost` | SQL Server + database, RabbitMQ (admin/admin, management plugin), `dashboard-agent-token` parameter, three projects | `Aspire.Hosting.SqlServer`, `Aspire.Hosting.RabbitMQ` 13.6.1 |
| `QuartzNetAspire.ServiceDefaults` | Aspire template; adds `AddSource("RabbitMQ.Client.*")` for publish/deliver spans | OpenTelemetry 1.18 |
| `QuartzNetAspire.Contracts` | `OrderPlaced` record; `EasyNetQConnection.FromAmqpUri` (Aspire hands out `amqp://user:pass@host:port`, EasyNetQ wants key=value or a `ConnectionConfiguration`) | `EasyNetQ` 8.1.11 |
| `QuartzNetAspire.ScheduledWorker` | `AddQuartzPersistentStore("quartz-scheduled", Clustered)` + `UseExecutionHistory()` + `AddJobLogScope()` + `UseDashboardAgent`; cron and simple triggers; durable `outbox-relay` with a `StartNow` trigger | `Quartz`, `Quartz.Aspire`, `Quartz.Dashboard.Agent` 4.6.0, `Microsoft.Data.SqlClient` 7.1.1 |
| `QuartzNetAspire.QueueWorker` | `UseInMemoryStore()` + `UseDashboardAgent`; durable `order-listener` with a `StartNow` trigger; `AddSource/AddMeter("Quartz")` by hand (no `Quartz.Aspire`) | `Quartz`, `Quartz.Dashboard.Agent` 4.6.0 |
| `QuartzNetAspire.Dashboard` | `AddQuartzDashboard(AttachStore("team-a") + AcceptAgents(token))`, `AddQuartzHttpApi("/quartz-api")`, `MapStaticAssets()`, `RequiresAspNetWebAssets` | `Quartz.Dashboard` 4.6.0 (brings `Quartz.AspNetCore`), `Microsoft.Data.SqlClient` |

### Dashboard connectivity modes compared

| | Store-attached (`AttachStore`) | Agent (`Quartz.Dashboard.Agent`) | HTTP target (`AddQuartzHttpClient`) |
|---|---|---|---|
| What the worker needs | nothing (dashboard needs DB credentials) | one package, outbound HTTPS to the dashboard, a token | `Quartz.AspNetCore` HTTP API mapped, inbound port, auth |
| Works with in-memory store | no | yes | yes |
| Scope | whole cluster, even nodes that are down; full DB history | one process / one scheduler per target | one process |
| Interrupt / Live Logs / Start-Standby-Shutdown | no (window) | yes | yes |
| Kubernetes fit | dashboard must reach every team's DB | best: no ingress, pod name = target | needs a Service per worker |
| Used here | Team A | Team A + Team B | not used, but the same API is mapped in the dashboard host for curl |

Recommendation for the real deployment: **agent on every worker** (it is the only mode that needs nothing inbound and works for in-memory schedulers), **plus** `AttachStore` for teams with a persistent store that want cluster-wide history and the Cluster page.

### Continuous jobs

Both continuous jobs follow the same shape:

```csharp
q.AddJob<OrderListenerJob>(j => j.WithIdentity("order-listener").StoreDurably());
q.AddTrigger(t => t.ForJob("order-listener").WithIdentity("order-listener-start").StartNow());
```

`Execute` subscribes (or loops) and awaits the job's `CancellationToken`. The dashboard's **Interrupt** fires that token; **Trigger now** starts a new run because the job is durable; the `StartNow` trigger is replaced on every process start, so a restarted pod restarts the job. `WaitForJobsToComplete = true` lets a `SIGTERM` finish the run cleanly.

What was tried and rejected: `RequestRecovery()` on the relay. After a *hard* kill (which is what `aspire resource stop` does on Windows) the fired-trigger row survives, cluster recovery creates a `recover_<instance>_<n>` trigger for the job, and because the restarted process has already re-fired its own `StartNow` trigger, every recovery trigger queues up **BLOCKED** behind the running job — four of them after four restarts, each one a future "run forever" once the current run is interrupted. For a job that never ends, recovery is the wrong tool; restart-on-startup is enough. (If "must always run somewhere in the cluster even while a pod is down" is required, use a repeating trigger with a do-nothing misfire instruction as a supervisor and stop the job with Pause + Interrupt instead.)

### Observability wiring

- `Quartz.Aspire` subscribes `QuartzInstrumentation.ActivitySourceName` / `MeterName` (both `"Quartz"`) on the host's `AddOpenTelemetry()`; ServiceDefaults owns the OTLP exporter. The queue worker, which has no `Quartz.Aspire`, adds the two lines itself. Do **not** use `OpenTelemetry.Instrumentation.Quartz` — it targets 3.x and silently emits nothing on 4.x.
- EasyNetQ 8 sits on RabbitMQ.Client 7, which has its own `RabbitMQ.Client.Publisher` / `.Subscriber` activity sources and propagates W3C trace context in message headers; one `AddSource("RabbitMQ.Client.*")` in ServiceDefaults is all the cross-worker tracing needs.
- A long-running job is one `Quartz.Job.Execute` span that never ends, so by default every publish inside the relay (and every delivery on the consumer) became a child of a single trace that grew for the life of the process. The relay therefore sets `Activity.Current = null` and starts its own `outbox.relay` root activity per order (`ActivitySource` named like the application, which ServiceDefaults already subscribes).
- `AddJobLogScope()` puts `quartz.job.name`, `quartz.trigger.name` and the fire instance id on every log line from a job.

## Notable environment facts

- Windows 11, .NET SDK 10.0.402 (an 11.0 RC is also installed, pinned away with `global.json`), Aspire CLI 13.6.1, **Podman** 6.1 (no Docker; Aspire auto-detects it).
- Packages (2026-10-20): Quartz / Quartz.Aspire / Quartz.Dashboard / Quartz.Dashboard.Agent 4.6.0, EasyNetQ 8.1.11, Aspire.Hosting.* 13.6.1, Microsoft.Data.SqlClient 7.1.1.
- `aspire resource <name> stop` terminates a project process without a graceful shutdown on Windows; that is what produced the recovery triggers and the misfire rows described above.

## Implementation plan (as executed)

1. **Research**: Quartz 4 docs for dashboard, agent, HTTP API, Aspire integration and OpenTelemetry; package versions from NuGet; the actual 4.6 API surface from the packages' XML docs (several doc snippets were out of date, e.g. `SimpleScheduleBuilder.WithIntervalInSeconds` no longer exists — `WithSimpleSchedule(TimeSpan)` repeats forever).
2. **Scaffold** by hand (no templates, the .NET 11 SDK was the default): `global.json`, `NuGet.config`, `aspire.config.json`, `.slnx`, six csproj files mirroring `src/AspireRustFS`; ServiceDefaults copied from there.
3. **Code**: AppHost wiring, Contracts, the five jobs, the dashboard host. First build green after two API fixes.
4. **Run and fix** (details below): schema not provisioned → launch settings; dashboard rendering; agent target collision; Interrupt through the window; recovery triggers; one giant trace.
5. **Verify** with the Aspire CLI (`aspire describe`, `aspire logs`, `aspire otel spans|traces|logs`), the Quartz HTTP API, `sqlcmd` inside the SQL container, `dotnet-counters collect --counters Quartz` on the worker process, and a throw-away Playwright script driving the Blazor dashboard (scheduler picker, Executing/Jobs/History/Cluster pages, Interrupt, Trigger now) — no interactive browser was available in the session.
6. **Docs**: this file and the README.

## Pitfalls found

| Symptom | Cause | Fix |
|---|---|---|
| Worker crashed at start: `Invalid object name 'QRTZ_JOB_DETAILS'` | Worker projects had no `launchSettings.json`, so they ran as `Production`; `Quartz.Aspire` only defaults `SchemaProvisioning` to `CreateIfMissing` in `Development` | `Properties/launchSettings.json` with `DOTNET_ENVIRONMENT=Development` and, explicitly, `settings.SchemaProvisioning = SchemaProvisioning.CreateIfMissing` (also creates the history tables: 49 schema objects) |
| Dashboard logged *store attached as 'team-a' could not be read* at start | The dashboard came up before the worker had provisioned the schema | Harmless: discovery retries every minute (`AttachStoreOptions.RediscoveryInterval`) and opened the window on the next round |
| Team B missing from the scheduler picker; worker log `refused registration: target 'boimler' is held by instance …` | Both agents used `Environment.MachineName` as `Target`; a target is held by one process | Prefix the target per worker (`team-a-<machine>`, `team-b-<machine>`); on Kubernetes the pod name alone is unique |
| `POST …/jobs/DEFAULT/outbox-relay/interrupt` returned `{"applied": false}` | The HTTP API resolves a bare name to the store-attached window, which cannot signal a process | Run the agent on Team A too; interrupt via the agent entry in the UI |
| Four `recover_*` triggers BLOCKED, Misfires = 6, "Total triggers 7" | `RequestRecovery()` on a never-ending job plus hard kills | Removed `RequestRecovery()`; purged the rows with `sqlcmd` |
| One trace with dozens of spans growing forever | Publishes nested under the relay's never-ending job span | `Activity.Current = null` + own root activity per relayed order |
| Blazor dashboard would 404 on `/_framework/blazor.web.js` | Host has no `.razor` files of its own | `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>` + `MapStaticAssets()` (from the docs, applied up front) |
| A stray `QuartzScheduler (not created)` in the picker | `builder.Services.AddQuartz()` in the dashboard host (copied from the docs sample) | Removed; `AddQuartzDashboard` needs no scheduler of its own, the window is added at runtime |
| EasyNetQ could not parse Aspire's connection string | Aspire injects `amqp://admin:…@localhost:port`; EasyNetQ expects `host=…;username=…` | `EasyNetQConnection.FromAmqpUri` → `ConnectionConfiguration` |
| `curl …/schedulers/team-b-queue/jobs` → 404 | The HTTP API does not address agent targets in 4.6 | By design; agent schedulers are UI-only (or run the HTTP API in the worker) |
| `{Amount:C}` logged `¤294.00` | Invariant culture in the worker | Log the raw decimal |
