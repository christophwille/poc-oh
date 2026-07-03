namespace TodoApp.AuthServer;

/// <summary>
/// Unauthenticated user-management endpoints for test seeding. Never expose these
/// in a real identity provider.
/// </summary>
public static class UserEndpoints
{
    public record CreateUserRequest(string SubjectId, string Username, string Password);

    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").AllowAnonymous();

        group.MapGet("/", (MutableUserStore store) =>
            Results.Ok(store.All.Select(u => new { u.SubjectId, u.Username })));

        group.MapPost("/", (CreateUserRequest request, MutableUserStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.SubjectId) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("subjectId, username and password are required.");
            }

            return store.Add(request.SubjectId, request.Username, request.Password)
                ? Results.Created($"/api/users/{request.Username}", new { request.SubjectId, request.Username })
                : Results.Conflict($"User '{request.Username}' already exists.");
        });

        group.MapDelete("/{username}", (string username, MutableUserStore store) =>
            store.Remove(username) ? Results.NoContent() : Results.NotFound());
    }
}
