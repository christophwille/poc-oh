using SagasWithWolverine.ApiService;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

// https://wolverinefx.net/guide/durability/sagas.html
//    https://wolverinefx.net/guide/durability/efcore/sagas
//    https://wolverinefx.net/guide/configuration

// https://www.milanjovanovic.tech/blog/implementing-the-saga-pattern-with-wolverine
// https://www.architecture-weekly.com/p/passive-aggresive-event

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("sagastate");

var configDebug = string.Join(Environment.NewLine,
    builder.Configuration.AsEnumerable()
        .OrderBy(kvp => kvp.Key)
        .Select(kvp => $"{kvp.Key} = {kvp.Value}"));

builder.Host.UseWolverine(options =>
{
    options.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .UseConventionalRouting();

    options.Policies.DisableConventionalLocalRouting();

    options.PersistMessagesWithSqlServer(connectionString!);
});


builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

if (app.Environment.IsDevelopment())
{
    app.MapGet("/config", () => configDebug);
}

app.MapPost("/orders/start", async (IMessageBus bus) =>
{
    var orderId = Guid.NewGuid().ToString("N");
    await bus.PublishAsync(new StartOrder(orderId));
    return Results.Accepted($"/orders/{orderId}", new { orderId });
});

app.MapPost("/orders/{id}/complete", async (string id, IMessageBus bus) =>
{
    await bus.PublishAsync(new CompleteOrder(id));
    return Results.Accepted();
});

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
