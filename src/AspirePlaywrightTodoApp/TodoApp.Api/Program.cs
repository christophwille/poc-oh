using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<TodoDbContext>("tododb");

// Browser-reachable auth server URL, injected by the AppHost via WithReference(authServer)
var authority = builder.Configuration["services:authserver:http:0"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.RequireHttpsMetadata = false; // all-HTTP sample on localhost
        options.MapInboundClaims = false;     // keep raw "sub"/"scope" claim types
        options.TokenValidationParameters.ValidateAudience = false;
    });

builder.Services.AddAuthorization(options =>
{
    // Duende emits one "scope" claim per scope, but tolerate space-delimited values too
    options.AddPolicy("todoapi", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.FindAll("scope")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains("todoapi")));
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();

// Runtime config for the Angular app (proxied through the dev server as /api/config)
app.MapGet("/api/config", (IConfiguration configuration) => Results.Ok(new
{
    authority = configuration["services:authserver:http:0"],
    clientId = "angular-spa",
    scope = "openid profile todoapi"
}));

var todos = app.MapGroup("/api/todos").RequireAuthorization("todoapi");

todos.MapGet("/", async (ClaimsPrincipal user, TodoDbContext db) =>
    await db.Todos
        .Where(t => t.UserId == user.FindFirstValue("sub"))
        .OrderBy(t => t.Id)
        .Select(t => new TodoDto(t.Id, t.Title, t.IsDone, t.CreatedAt))
        .ToListAsync());

todos.MapPost("/", async (CreateTodoRequest request, ClaimsPrincipal user, TodoDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest("title is required.");
    }

    var todo = new TodoItem
    {
        UserId = user.FindFirstValue("sub")!,
        Title = request.Title.Trim(),
        CreatedAt = DateTime.UtcNow
    };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/api/todos/{todo.Id}", new TodoDto(todo.Id, todo.Title, todo.IsDone, todo.CreatedAt));
});

todos.MapPut("/{id:int}", async (int id, UpdateTodoRequest request, ClaimsPrincipal user, TodoDbContext db) =>
{
    var todo = await db.Todos.SingleOrDefaultAsync(t => t.Id == id && t.UserId == user.FindFirstValue("sub"));
    if (todo is null)
    {
        return Results.NotFound();
    }

    todo.Title = string.IsNullOrWhiteSpace(request.Title) ? todo.Title : request.Title.Trim();
    todo.IsDone = request.IsDone;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

todos.MapDelete("/{id:int}", async (int id, ClaimsPrincipal user, TodoDbContext db) =>
{
    var deleted = await db.Todos
        .Where(t => t.Id == id && t.UserId == user.FindFirstValue("sub"))
        .ExecuteDeleteAsync();

    return deleted > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();

internal record TodoDto(int Id, string Title, bool IsDone, DateTime CreatedAt);
internal record CreateTodoRequest(string Title);
internal record UpdateTodoRequest(string? Title, bool IsDone);
