using AzSqlContainerTodoApp.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AzSqlContainerTodoApp.Api.Features.Todos;

public static class SetTodoCompleted
{
    public record Request(bool IsCompleted);

    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/{id:int}/completed", Handle);

    public static async Task<Results<NoContent, NotFound>> Handle(
        int id, Request request, TodoDbContext db, CancellationToken ct)
    {
        var updated = await db.Todos.Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsCompleted, request.IsCompleted), ct);

        return updated == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
}
