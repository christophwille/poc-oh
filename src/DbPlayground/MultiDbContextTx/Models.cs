using Microsoft.EntityFrameworkCore;

namespace MultiDbContextTx;

/*
dotnet ef migrations add InitialBlogging --context SqlServerBloggingContext --output-dir Migrations/Bloggingl --verbose
dotnet ef migrations add InitialEmail --context SqlServerEmailContext --output-dir Migrations/Emaill --verbose
*/

// https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli
public class SqlServerBloggingContext : DbContext
{
    public SqlServerBloggingContext()
    {
    }
    public SqlServerBloggingContext(DbContextOptions<SqlServerBloggingContext> options) : base(options)
    {
    }

    public string ConnectionString { get; set; } = "data source=.;initial catalog=multitxsample;MultipleActiveResultSets=True;Trusted_Connection=True;Trust Server Certificate=true";

    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured) options.UseSqlServer(ConnectionString);
        base.OnConfiguring(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("blog");
        base.OnModelCreating(modelBuilder);
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

public class SqlServerEmailContext : DbContext
{
    public SqlServerEmailContext()
    {
    }
    public SqlServerEmailContext(DbContextOptions<SqlServerEmailContext> options) : base(options)
    {
    }

    public string ConnectionString { get; set; } = "data source=.;initial catalog=multitxsample;MultipleActiveResultSets=True;Trusted_Connection=True;Trust Server Certificate=true";

    public DbSet<Email> Emails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured) options.UseSqlServer(ConnectionString);
        base.OnConfiguring(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("mail");
        base.OnModelCreating(modelBuilder);
    }
}

public class Email
{
    public int EmailId { get; set; }
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}