# TodoApp — Aspire sample with Angular, IdentityServer and Playwright E2E tests

An [Aspire](https://aspire.dev) 13 (.NET 10) sample demonstrating a token-based SPA architecture:

- **TodoApp.Api** — minimal API with per-user todo CRUD, backed by **SQL Server + EF Core**, protected by JWT bearer tokens.
- **frontend/** — **Angular 21** SPA that performs the OIDC **authorization code + PKCE** login itself; the access token lives in the browser and is sent as a `Bearer` header on API calls (no backend session cookie).
- **TodoApp.AuthServer** — **Duende IdentityServer 8** with clients/scopes hardcoded in C# and an in-memory user list that can be mutated through unauthenticated `/api/users` endpoints (test seeding only — never do this in production).
- **TodoApp.MigrationService** — worker that applies EF migrations and seeds sample todos for `alice`/`bob`, then exits.
- **TodoApp.E2ETests** — xUnit + **Playwright** tests that boot the entire distributed app via `Aspire.Hosting.Testing`, drive a real browser login, and validate created todos directly in the SQL database.

## Prerequisites

- .NET 10 SDK
- Node.js 20+ (Angular CLI not required; `npm` scripts are used)
- A container runtime (Docker Desktop or Podman) for the SQL Server container
- Playwright browsers for the tests: `pwsh TodoApp.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium` (after the first build)

## Run

```powershell
dotnet run --project TodoApp.AppHost --launch-profile http
```

Open the dashboard URL printed to the console, then browse to the **frontend** resource and sign in with `user1`/`P@ssw0rd1!` (or the seeded `alice`/`alice`, `bob`/`bob`).

### Browser logs in the dashboard

The frontend has `.WithBrowserLogs()` (from the experimental `Aspire.Hosting.Browsers` preview package), which adds a `frontend-browser-logs` child resource. Run the **"Open tracked browser"** command on it in the dashboard to launch a CDP-tracked Chromium-family browser (requires Edge or Chrome installed): browser console output, uncaught errors, and network failures then stream into the dashboard's console logs, and a **"Capture screenshot"** command is available. The resource stays `NotStarted` until you trigger it, so tests and headless runs are unaffected.

## Test

```powershell
dotnet test TodoApp.E2ETests
```

The first run is slow (SQL image pull, `npm ci`, `ng serve` build). Tests create their own users through the auth server's seeding endpoints, so they are independent of each other.

The test project uses **xUnit.net v3 on Microsoft Testing Platform** (`UseMicrosoftTestingPlatformRunner`), so it is also a self-contained executable: `bin/Debug/net10.0/TodoApp.E2ETests.exe --list-tests`. With `dotnet test`, pass platform arguments after `--` (e.g. `dotnet test TodoApp.E2ETests -- --filter-method "*FreshUser*"`); the VSTest `--filter` syntax no longer applies.

## How the dynamic wiring works

Aspire assigns random ports in test runs, so nothing is hardcoded:

- The AppHost passes the frontend's origin to the auth server as `FRONTEND_ORIGIN`; the C# client config reads it for `RedirectUris`/`AllowedCorsOrigins`.
- The API exposes an anonymous `GET /api/config` returning the auth server URL; the Angular app bootstraps its OIDC config from it (`StsConfigHttpLoader`).
- The Angular dev server proxies `/api` to the API (`proxy.conf.js` reads the Aspire-injected `services__api__http__0`), so the API needs no CORS.

Everything runs on plain HTTP on localhost. Note that the IdentityServer session cookie is configured with `SameSite=Lax` (`CookieSameSiteMode`) because Chromium rejects the Duende default (`SameSite=None`) on non-HTTPS origins.
