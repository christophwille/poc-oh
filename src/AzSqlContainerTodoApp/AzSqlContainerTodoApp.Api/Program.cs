using AzSqlContainerTodoApp.Api.Data;
using AzSqlContainerTodoApp.Api.Features.Todos;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<TodoDbContext>("tododb");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // shortcut: no migrations, switch to EF migrations when the schema changes
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<TodoDbContext>().Database.EnsureCreatedAsync();
}

var todos = app.MapGroup("/todos");
CreateTodo.Map(todos);
ListTodos.Map(todos);
SetTodoCompleted.Map(todos);
DeleteTodo.Map(todos);

app.MapDefaultEndpoints();

app.Run();
