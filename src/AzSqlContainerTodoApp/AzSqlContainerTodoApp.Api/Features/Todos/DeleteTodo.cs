using AzSqlContainerTodoApp.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AzSqlContainerTodoApp.Api.Features.Todos;

public static class DeleteTodo
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapDelete("/{id:int}", Handle);

    public static async Task<Results<NoContent, NotFound>> Handle(int id, TodoDbContext db, CancellationToken ct)
    {
        var deleted = await db.Todos.Where(t => t.Id == id).ExecuteDeleteAsync(ct);

        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
}
