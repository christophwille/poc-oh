using Microsoft.EntityFrameworkCore;

namespace TodoApp.Data;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.Property(t => t.UserId).HasMaxLength(200);
            entity.Property(t => t.Title).HasMaxLength(500);
            entity.HasIndex(t => t.UserId);
        });
    }
}
