using AzSqlContainerTodoApp.Api.Data;
using AzSqlContainerTodoApp.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(AzSqlFixture))]

namespace AzSqlContainerTodoApp.IntegrationTests;

// One Azure SQL Database container for the whole test run.
public sealed class AzSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/azure-sql-db-server:latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.EnsureCreatedAsync(); // creates the tododb database and the schema
    }

    public TodoDbContext CreateContext() => new(new DbContextOptionsBuilder<TodoDbContext>()
        .UseSqlServer(_container.GetConnectionString().Replace("Database=master", "Database=tododb"))
        .Options);

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
