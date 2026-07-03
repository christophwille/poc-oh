#pragma warning disable ASPIREBROWSERLOGS001 // WithBrowserLogs is experimental (evaluation only)

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql");
var db = sql.AddDatabase("tododb");

var authServer = builder.AddProject<Projects.TodoApp_AuthServer>("authserver", launchProfileName: "http")
    .WithHttpHealthCheck("/.well-known/openid-configuration")
    .WithExternalHttpEndpoints();

var migrations = builder.AddProject<Projects.TodoApp_MigrationService>("migrations")
    .WithReference(db)
    .WaitFor(db);

var api = builder.AddProject<Projects.TodoApp_Api>("api", launchProfileName: "http")
    .WithReference(db)
    .WithReference(authServer)
    .WithHttpHealthCheck("/health")
    .WaitForCompletion(migrations)
    .WaitFor(authServer);

var frontend = builder.AddJavaScriptApp("frontend", "../frontend", runScriptName: "start")
    .WithNpm(installCommand: "ci")
    .WithHttpEndpoint(env: "PORT")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpHealthCheck("/")
    .WithExternalHttpEndpoints()
    .WithBrowserLogs(); // dashboard-launched tracked browser: console logs, errors, screenshots

// The SPA client's redirect/CORS origin; resolves to the allocated frontend URL.
authServer.WithEnvironment("FRONTEND_ORIGIN", frontend.GetEndpoint("http"));

builder.Build().Run();
