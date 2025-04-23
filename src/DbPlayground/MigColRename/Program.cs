using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

// https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli
Console.WriteLine("Hello, World!");


public class BlogContext : DbContext
{
    public BlogContext() : base()
    {
    }

    public BlogContext(DbContextOptions<BlogContext> context) : base(context)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=someremoteservernotreachable;Database=EFTesting;Trusted_Connection=True;MultipleActiveResultSets=true");
    }

    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}

public class Blog
{
    public int BlogId { get; set; }
    public string Name1 { get; set; } // We'll use that column for renaming
    public string Url { get; set; }
    public List<Post> Posts { get; set; }
}

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int BlogId { get; set; }
    public Blog Blog { get; set; }
}

// https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#from-a-design-time-factory
public class BloggingContextFactory : IDesignTimeDbContextFactory<BlogContext>
{
    public BlogContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BlogContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=BlogsSample;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new BlogContext(optionsBuilder.Options);
    }
}