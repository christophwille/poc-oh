using AzSqlContainerTodoApp.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AzSqlContainerTodoApp.Api.Features.Todos;

public static class ListTodos
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/", Handle);

    // completed: null = all, true/false = filter
    public static async Task<Ok<List<TodoResponse>>> Handle(bool? completed, TodoDbContext db, CancellationToken ct)
    {
        var todos = await db.Todos.AsNoTracking()
            .Where(t => completed == null || t.IsCompleted == completed)
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
            .Select(t => new TodoResponse(t.Id, t.Title, t.IsCompleted, t.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(todos);
    }
}
