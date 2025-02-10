using Microsoft.EntityFrameworkCore;

namespace SampleStuff.ToTest;

// https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli
public class SqlServerBloggingContext : DbContext
{
    public SqlServerBloggingContext()
    {
    }
    public SqlServerBloggingContext(DbContextOptions<SqlServerBloggingContext> options) : base(options)
    {
    }

    public string ConnectionString { get; set; } = "Server=(localdb)\\mssqllocaldb;Database=BloggingDemo;Trusted_Connection=True;MultipleActiveResultSets=true";

    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured) options.UseSqlServer(ConnectionString);
        base.OnConfiguring(options);
    }
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