using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Optional: the same data as JSON (curl-able; what the dashboard's own HTTP-target mode would consume).
builder.Services.AddQuartzHttpApi(options => options.ApiPath = "/quartz-api");

builder.Services.AddQuartzDashboard(options =>
{
    // Team A: read their SQL Server job store directly (every scheduler in that database shows up).
    options.AttachStore("team-a", store =>
    {
        store.UseSqlServer(builder.Configuration.GetConnectionString("quartz-scheduled")!);
        store.UseExecutionHistory(); // history rows are written by the workers; must be enabled on both sides
    });

    // Team B (and anyone else): workers dial in with Quartz.Dashboard.Agent using this shared token.
    options.AcceptAgents(agents => agents.Tokens.Primary = builder.Configuration["Dashboard:AgentToken"]);

    // The dashboard can schedule any job type by name; narrow it to this solution's jobs.
    options.IsJobTypeAllowed = typeName => typeName.StartsWith("QuartzNetAspire.", StringComparison.Ordinal);
});

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();

// Demo only: no authentication. In production put RequireAuthorization() behind the company IdP.
app.MapQuartzDashboard().AllowAnonymous();
app.MapQuartzHttpApi().AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/quartz"));
app.MapDefaultEndpoints();

app.Run();
