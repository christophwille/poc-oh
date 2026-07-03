using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace TodoApp.E2ETests;

[Collection(AppCollection.Name)]
public class TodoE2ETests(AppFixture fixture) : PageTest
{
    private static readonly PageWaitForURLOptions NavigationTimeout = new() { Timeout = 30_000 };

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        IgnoreHTTPSErrors = true,
    };

    private void AttachBrowserDiagnostics()
    {
        Page.Console += (_, msg) => Console.WriteLine($"[browser:{msg.Type}] {msg.Text}");
        Page.PageError += (_, err) => Console.WriteLine($"[browser:pageerror] {err}");
        Page.RequestFailed += (_, req) => Console.WriteLine($"[browser:requestfailed] {req.Method} {req.Url} -> {req.Failure}");
        Page.Response += (_, res) =>
        {
            if (res.Status >= 400)
            {
                Console.WriteLine($"[browser:response] {res.Status} {res.Url}");
            }
        };
    }

    private async Task LoginAsync(string username, string password)
    {
        AttachBrowserDiagnostics();
        await Page.GotoAsync(fixture.FrontendUrl);
        await Page.GetByTestId("login").ClickAsync(new() { Timeout = 30_000 });

        // Full-page redirect to the IdentityServer quickstart UI
        await Page.WaitForURLAsync($"{fixture.AuthServerUrl}/**", NavigationTimeout);
        await Page.FillAsync("input[name='Input.Username']", username);
        await Page.FillAsync("input[name='Input.Password']", password);
        await Page.ClickAsync("button[value='login']");

        // Back on the SPA; checkAuth() exchanges the code for tokens
        await Page.WaitForURLAsync($"{fixture.FrontendUrl}/**", NavigationTimeout);
        await Expect(Page.GetByTestId("user-name")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    private async Task<(string Sub, string Username)> CreateFreshUserAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sub = $"sub-{suffix}";
        var username = $"user-{suffix}";
        await fixture.CreateUserAsync(sub, username, "Passw0rd!");
        return (sub, username);
    }

    [Fact]
    public async Task FreshUser_StartsEmpty_CreatedTodoPersistsToSql()
    {
        var (sub, username) = await CreateFreshUserAsync();
        await LoginAsync(username, "Passw0rd!");

        await Expect(Page.GetByTestId("todo-item")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("empty-list")).ToBeVisibleAsync();

        await Page.GetByTestId("new-todo-title").FillAsync("Buy milk");
        await Page.GetByTestId("add-todo").ClickAsync();

        await Expect(Page.GetByTestId("todo-item")).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId("todo-item")).ToContainTextAsync("Buy milk");

        // The todo really landed in SQL Server, owned by this user's subject
        await fixture.WaitForTodoCountAsync(1, sub, title: "Buy milk");
    }

    [Fact]
    public async Task SeededUser_Alice_SeesSeededTodos()
    {
        await LoginAsync("alice", "alice");

        var items = Page.GetByTestId("todo-item");
        await Expect(items).ToHaveCountAsync(3);
        await Expect(items.Filter(new() { HasText = "Buy groceries" })).ToHaveCountAsync(1);
        await Expect(items.Filter(new() { HasText = "Walk the dog" })).ToHaveCountAsync(1);
        await Expect(items.Filter(new() { HasText = "Read the Aspire docs" })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ToggleAndDelete_AreReflectedInSql()
    {
        var (sub, username) = await CreateFreshUserAsync();
        await LoginAsync(username, "Passw0rd!");

        await Page.GetByTestId("new-todo-title").FillAsync("Ship it");
        await Page.GetByTestId("add-todo").ClickAsync();
        await Expect(Page.GetByTestId("todo-item")).ToHaveCountAsync(1);
        await fixture.WaitForTodoCountAsync(1, sub, title: "Ship it", isDone: false);

        await Page.GetByTestId("toggle-todo").CheckAsync();
        await fixture.WaitForTodoCountAsync(1, sub, title: "Ship it", isDone: true);

        await Page.GetByTestId("delete-todo").ClickAsync();
        await Expect(Page.GetByTestId("todo-item")).ToHaveCountAsync(0);
        await fixture.WaitForTodoCountAsync(0, sub);
    }

    [Fact]
    public async Task RemovedUser_CannotLogin()
    {
        var (_, username) = await CreateFreshUserAsync();
        await fixture.DeleteUserAsync(username);

        await Page.GotoAsync(fixture.FrontendUrl);
        await Page.GetByTestId("login").ClickAsync(new() { Timeout = 30_000 });
        await Page.WaitForURLAsync($"{fixture.AuthServerUrl}/**", NavigationTimeout);
        await Page.FillAsync("input[name='Input.Username']", username);
        await Page.FillAsync("input[name='Input.Password']", "Passw0rd!");
        await Page.ClickAsync("button[value='login']");

        // Still on the login page, showing the invalid-credentials error
        await Expect(Page.Locator(".alert-danger"))
            .ToContainTextAsync("Invalid username or password", new() { Timeout = 15_000 });
        Assert.StartsWith(fixture.AuthServerUrl, Page.Url);
    }
}
