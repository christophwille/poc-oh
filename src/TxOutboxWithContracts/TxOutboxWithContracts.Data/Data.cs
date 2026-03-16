using Microsoft.EntityFrameworkCore;
using SmartEnum.EFCore;
using System.Text.Json;

namespace TxOutboxWithContracts.Data;

public class SqlServerBloggingContext : DbContext
{
    public DbSet<OutboxMessage> Outbox { get; set; }

    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer("Data Source=.;Initial Catalog=poc.outbox;Integrated Security=True;TrustServerCertificate=True");

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // https://github.com/ardalis/SmartEnum?tab=readme-ov-file#using-smartenumefcore
        configurationBuilder.ConfigureSmartEnum();

        base.ConfigureConventions(configurationBuilder);
    }

    public void AddOutboxMessage<T>(T message)
    {
        var outboxMessage = OutboxMessage.Create<T>(message);
        this.Outbox.Add(outboxMessage);
    }

}

public sealed class OutboxMessage
{
    public static OutboxMessage Create<T>(T message)
    {
        return new OutboxMessage
        {
            Type = typeof(T).FullName,
            Data = JsonSerializer.Serialize(message),
            CreatedOnUtc = DateTime.UtcNow
        };
    }

    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }

    // Not used atm
    public string? Error { get; set; }

    // Could add: a .Status enum, a .Retries count, a .NextRetryOnUtc, etc
}

public class Blog
{
    public int BlogId { get; set; }
    public string Url { get; set; }

    public List<Post> Posts { get; } = new();
}

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    public int BlogId { get; set; }
    public Blog Blog { get; set; }
}