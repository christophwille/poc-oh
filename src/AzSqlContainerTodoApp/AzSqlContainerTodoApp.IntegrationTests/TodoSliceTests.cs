using AzSqlContainerTodoApp.Api.Data;
using AzSqlContainerTodoApp.Api.Features.Todos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AzSqlContainerTodoApp.IntegrationTests;

// Every test runs in a transaction that is never committed, so tests never see rows of other tests.
public sealed class TodoSliceTests(AzSqlFixture fixture) : IAsyncLifetime
{
    private readonly TodoDbContext _db = fixture.CreateContext();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await _db.Database.BeginTransactionAsync(Ct);

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private async Task<TodoResponse> Create(string title)
    {
        var result = await CreateTodo.Handle(new(title), _db, Ct);
        return Assert.IsType<Created<TodoResponse>>(result.Result).Value!;
    }

    [Fact]
    public async Task Container_is_Azure_SQL_Database()
    {
        var edition = await _db.Database.SqlQuery<int>(
            $"SELECT CAST(SERVERPROPERTY('EngineEdition') AS int) AS Value").SingleAsync(Ct);

        Assert.Equal(5, edition); // 5 = Azure SQL Database
    }

    [Fact]
    public async Task Create_persists_trimmed_todo()
    {
        var created = await Create("  buy milk  ");

        var stored = await _db.Todos.AsNoTracking().SingleAsync(t => t.Id == created.Id, Ct);
        Assert.Equal("buy milk", stored.Title);
        Assert.False(stored.IsCompleted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData(Todo.TitleMaxLength + 1)]
    public async Task Create_rejects_invalid_title_and_writes_nothing(object? title)
    {
        var text = title is int length ? new string('x', length) : (string?)title;

        var result = await CreateTodo.Handle(new(text), _db, Ct);

        Assert.IsType<ValidationProblem>(result.Result);
        Assert.Empty(await _db.Todos.ToListAsync(Ct));
    }

    [Fact]
    public async Task Create_accepts_title_of_max_length()
    {
        var created = await Create(new string('x', Todo.TitleMaxLength));

        Assert.Equal(Todo.TitleMaxLength, created.Title.Length);
    }

    [Fact]
    public async Task List_returns_todos_in_creation_order_and_filters_by_completed()
    {
        var first = await Create("first");
        var second = await Create("second");
        await SetTodoCompleted.Handle(second.Id, new(true), _db, Ct);

        var all = (await ListTodos.Handle(null, _db, Ct)).Value!;
        var done = (await ListTodos.Handle(true, _db, Ct)).Value!;
        var open = (await ListTodos.Handle(false, _db, Ct)).Value!;

        Assert.Equal([first.Id, second.Id], all.Select(t => t.Id));
        Assert.Equal([second.Id], done.Select(t => t.Id));
        Assert.Equal([first.Id], open.Select(t => t.Id));
    }

    [Fact]
    public async Task SetCompleted_updates_row_and_can_be_reverted()
    {
        var todo = await Create("toggle me");

        Assert.IsType<NoContent>((await SetTodoCompleted.Handle(todo.Id, new(true), _db, Ct)).Result);
        Assert.True(await _db.Todos.Where(t => t.Id == todo.Id).Select(t => t.IsCompleted).SingleAsync(Ct));

        Assert.IsType<NoContent>((await SetTodoCompleted.Handle(todo.Id, new(false), _db, Ct)).Result);
        Assert.False(await _db.Todos.Where(t => t.Id == todo.Id).Select(t => t.IsCompleted).SingleAsync(Ct));
    }

    [Fact]
    public async Task SetCompleted_returns_NotFound_for_unknown_id() =>
        Assert.IsType<NotFound>((await SetTodoCompleted.Handle(int.MaxValue, new(true), _db, Ct)).Result);

    [Fact]
    public async Task Delete_removes_row()
    {
        var todo = await Create("delete me");

        Assert.IsType<NoContent>((await DeleteTodo.Handle(todo.Id, _db, Ct)).Result);
        Assert.False(await _db.Todos.AnyAsync(t => t.Id == todo.Id, Ct));
    }

    [Fact]
    public async Task Delete_returns_NotFound_for_unknown_id() =>
        Assert.IsType<NotFound>((await DeleteTodo.Handle(int.MaxValue, _db, Ct)).Result);
}
