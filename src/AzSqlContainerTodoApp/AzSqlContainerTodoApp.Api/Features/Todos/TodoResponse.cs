namespace AzSqlContainerTodoApp.Api.Features.Todos;

public record TodoResponse(int Id, string Title, bool IsCompleted, DateTimeOffset CreatedAt);
