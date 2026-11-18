# AzSqlContainerTodoApp

A small ASP.NET Core Todo API (vertical slices, EF Core 11, no authentication) whose database/business layer is
integration tested with Testcontainers against the **Azure SQL Database container**
`mcr.microsoft.com/mssql/azure-sql-db-server:latest` (public preview, linux/amd64 and linux/arm64).
Aspire 17 runs the same image for local development.

Background: [Build locally with Azure SQL Database container](https://devblogs.microsoft.com/azure-sql/build-locally-with-azure-sql-database-container-now-in-public-preview/)

## Projects

| Project | Purpose |
| --- | --- |
| `AzSqlContainerTodoApp.Api` | Minimal API. One file per feature in `Features/Todos`; each has a static `Handle` method (the business logic) and a `Map` method (the route). |
| `AzSqlContainerTodoApp.IntegrationTests` | xUnit v3 tests that call the `Handle` methods against a real Azure SQL container. |
| `AzSqlContainerTodoApp.AppHost` | Aspire 17 AppHost: SQL container with the Azure SQL image, database `tododb`, the API. |
| `AzSqlContainerTodoApp.ServiceDefaults` | Stock Aspire service defaults (telemetry, health checks). |

## Run the integration tests

Needs the .NET 11 SDK and a running container runtime (Docker or Podman).

```
dotnet test
```

The first run pulls the image, which takes several minutes. `AzSqlFixture` starts one container for the whole run
and creates the schema with `EnsureCreated`. Every test runs in a transaction that is never committed, so tests do
not see each other's data. `Container_is_Azure_SQL_Database` asserts `SERVERPROPERTY('EngineEdition') = 5`, which
proves the tests are talking to the Azure SQL engine and not to a regular SQL Server image.

## Run the app with Aspire

```
aspire run
```

This needs the Aspire 17 CLI (`aspire update --self`); the 13.6 CLI hangs at "Building AppHost" because the AppHost
no longer uses `Aspire.AppHost.Sdk` (obsolete in 17). `dotnet run --project AzSqlContainerTodoApp.AppHost` works
without the CLI. Then, using the `api` URL from the dashboard (http://localhost:5012):

```
curl -X POST {api}/todos -H "Content-Type: application/json" -d '{"title":"buy milk"}'
curl {api}/todos
curl "{api}/todos?completed=false"
curl -X PUT {api}/todos/1/completed -H "Content-Type: application/json" -d '{"isCompleted":true}'
curl -X DELETE {api}/todos/1
```

## Notes

- The schema is created with `EnsureCreated` (in Development at API startup, and in the test fixture). There are no EF migrations.
- Like Azure SQL Database, the container rejects `USE <database>` (Msg 40508). Select the database in the connection string.
- Podman on Windows: Testcontainers needs the Docker-compatible pipe `\\.\pipe\docker_engine`. If `podman machine start`
  reports `win-sshproxy.exe failed to start`, stop the machine, delete the stale
  `%TEMP%\podman\podman-machine-default-api.sock` and start it again.
