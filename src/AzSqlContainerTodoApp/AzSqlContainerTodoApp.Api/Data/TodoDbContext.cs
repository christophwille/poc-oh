using Microsoft.EntityFrameworkCore;

namespace AzSqlContainerTodoApp.Api.Data;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Todo>().Property(t => t.Title).HasMaxLength(Todo.TitleMaxLength);
}
