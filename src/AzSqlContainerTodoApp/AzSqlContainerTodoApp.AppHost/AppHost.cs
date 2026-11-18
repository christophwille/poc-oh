var builder = DistributedApplication.CreateBuilder(args);

// Azure SQL Database container (public preview, amd64 + arm64) instead of the default mssql/server image.
var sql = builder.AddSqlServer("sql")
    .WithImageRegistry("mcr.microsoft.com")
    .WithImage("mssql/azure-sql-db-server", "latest");

var db = sql.AddDatabase("tododb");

builder.AddDotnetProject("api", "../AzSqlContainerTodoApp.Api/AzSqlContainerTodoApp.Api.csproj")
    .WithReference(db)
    .WaitFor(db)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
