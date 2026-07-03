using Microsoft.EntityFrameworkCore;
using TodoApp.Data;

namespace TodoApp.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await db.Database.MigrateAsync(stoppingToken);
            await SeedAsync(db, stoppingToken);
        });

        logger.LogInformation("Database migrated and seeded");
        lifetime.StopApplication();
    }

    private static async Task SeedAsync(TodoDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Todos.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.Todos.AddRange(
            new TodoItem { UserId = "alice", Title = "Buy groceries", CreatedAt = now },
            new TodoItem { UserId = "alice", Title = "Walk the dog", CreatedAt = now },
            new TodoItem { UserId = "alice", Title = "Read the Aspire docs", IsDone = true, CreatedAt = now },
            new TodoItem { UserId = "bob", Title = "Book flights", CreatedAt = now },
            new TodoItem { UserId = "bob", Title = "Water the plants", CreatedAt = now });

        await db.SaveChangesAsync(cancellationToken);
    }
}
