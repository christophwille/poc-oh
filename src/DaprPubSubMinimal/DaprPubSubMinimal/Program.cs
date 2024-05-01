using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();

var useManSidekick = builder.Configuration.GetSection("DaprSidekick").Exists();
if (useManSidekick)
{
    Console.WriteLine("Using Sidekick");
    builder.Services.AddDaprSidekick(builder.Configuration);
}

var app = builder.Build();

app.UseCloudEvents();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapGet("/dapr/subscribe", () =>
{
    return Results.Ok(new[] {
                new {
                    pubsubname = "demopubsub",
                    topic = "demotopic",
                    route = "sub"
                }});
});

app.MapPost("sub", async ([FromBody] DemoPubSubMessage pubSubMessage, [FromServices] ILogger<Program> logger) =>
{
    logger.LogInformation("sub: entering " + DateTime.UtcNow.Ticks);

    await Task.Delay(10000); // simulate work and make sure to avoid this is short

    logger.LogInformation("sub: leaving " + DateTime.UtcNow.Ticks);

    // return Results.NotFound(); // for "delete message"
    // return Results.UnprocessableEntity() // for "retry message"
    return Results.Ok(); // for "message processed"
});

app.MapPost("enqueue", async ([FromServices] DaprClient daprClient, [FromServices] ILogger<Program> logger, [FromBody] DemoEndpointRequest request) =>
{
    logger.LogInformation("enqueue: entering " + DateTime.UtcNow.Ticks);

    await daprClient.PublishEventAsync("demopubsub", "demotopic", new DemoPubSubMessage(request.Message));

    logger.LogInformation("enqueue: after PublishEventAsync " + DateTime.UtcNow.Ticks);

    return Results.Ok();
});

app.Run();

public record DemoPubSubMessage(string Message);
public record DemoEndpointRequest(string Message);