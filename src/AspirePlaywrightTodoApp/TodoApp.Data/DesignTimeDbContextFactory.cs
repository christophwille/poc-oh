using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TodoApp.Data;

// Lets `dotnet ef` create migrations without a running application host.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TodoDbContext>
{
    public TodoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseSqlServer("Server=localhost;Database=tododb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new TodoDbContext(options);
    }
}
