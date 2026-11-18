namespace AzSqlContainerTodoApp.Api.Data;

public class Todo
{
    public const int TitleMaxLength = 200;

    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
