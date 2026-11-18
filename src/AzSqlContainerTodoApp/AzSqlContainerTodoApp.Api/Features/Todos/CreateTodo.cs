using AzSqlContainerTodoApp.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AzSqlContainerTodoApp.Api.Features.Todos;

public static class CreateTodo
{
    public record Request(string? Title);

    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/", Handle);

    public static async Task<Results<Created<TodoResponse>, ValidationProblem>> Handle(
        Request request, TodoDbContext db, CancellationToken ct)
    {
        var title = request.Title?.Trim();
        if (string.IsNullOrEmpty(title) || title.Length > Todo.TitleMaxLength)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["title"] = [$"Title is required and must be at most {Todo.TitleMaxLength} characters."]
            });
        }

        var todo = new Todo { Title = title, CreatedAt = DateTimeOffset.UtcNow };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/todos/{todo.Id}",
            new TodoResponse(todo.Id, todo.Title, todo.IsCompleted, todo.CreatedAt));
    }
}
