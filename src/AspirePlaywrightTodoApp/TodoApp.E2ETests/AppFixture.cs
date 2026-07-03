using System.Net.Http.Json;
using Aspire.Hosting;
using Microsoft.Data.SqlClient;

namespace TodoApp.E2ETests;

/// <summary>
/// Boots the whole distributed application (SQL Server container, auth server, API,
/// migrations, Angular dev server) once for the test collection.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    private DistributedApplication? _app;
    private readonly HttpClient _http = new();

    public string FrontendUrl { get; private set; } = string.Empty;
    public string AuthServerUrl { get; private set; } = string.Empty;
    public string DbConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        using var cts = new CancellationTokenSource(StartupTimeout);
        var ct = cts.Token;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TodoApp_AppHost>(ct);
        builder.Services.ConfigureHttpClientDefaults(b => b.AddStandardResilienceHandler());

        _app = await builder.BuildAsync(ct);
        await _app.StartAsync(ct);

        await _app.ResourceNotifications.WaitForResourceAsync("migrations", KnownResourceStates.Finished, ct);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("authserver", ct);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api", ct);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("frontend", ct);

        FrontendUrl = _app.GetEndpoint("frontend").ToString().TrimEnd('/');
        AuthServerUrl = _app.GetEndpoint("authserver").ToString().TrimEnd('/');
        DbConnectionString = await _app.GetConnectionStringAsync("tododb", ct)
            ?? throw new InvalidOperationException("No connection string for 'tododb'.");
    }

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    public async Task CreateUserAsync(string subjectId, string username, string password)
    {
        var response = await _http.PostAsJsonAsync(
            $"{AuthServerUrl}/api/users",
            new { subjectId, username, password });
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(string username)
    {
        var response = await _http.DeleteAsync($"{AuthServerUrl}/api/users/{username}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> CountTodosAsync(string subjectId, string? title = null, bool? isDone = null)
    {
        await using var connection = new SqlConnection(DbConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Todos WHERE UserId = @userId" +
                              (title is null ? "" : " AND Title = @title") +
                              (isDone is null ? "" : " AND IsDone = @isDone");
        command.Parameters.AddWithValue("@userId", subjectId);
        if (title is not null)
        {
            command.Parameters.AddWithValue("@title", title);
        }
        if (isDone is not null)
        {
            command.Parameters.AddWithValue("@isDone", isDone.Value);
        }

        return (int)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Polls the database until the row count matches (UI actions are async).</summary>
    public async Task WaitForTodoCountAsync(
        int expected, string subjectId, string? title = null, bool? isDone = null,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        int actual;
        do
        {
            actual = await CountTodosAsync(subjectId, title, isDone);
            if (actual == expected)
            {
                return;
            }
            await Task.Delay(250);
        } while (DateTime.UtcNow < deadline);

        Assert.Fail($"Expected {expected} todo(s) for '{subjectId}' (title: {title ?? "*"}, isDone: {isDone?.ToString() ?? "*"}), found {actual}.");
    }
}

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "app";
}
