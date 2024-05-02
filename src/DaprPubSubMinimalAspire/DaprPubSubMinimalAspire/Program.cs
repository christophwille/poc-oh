using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

// Read https://anthonysimmon.com/dotnet-aspire-best-way-to-experiment-dapr-local-dev/
// Modified from DaprPubSubMinimal/Program.cs
// Automatically added by wizard: AddServiceDefaults() and MapDefaultEndpoints()
// Manually removed: https redirection, man sidekick

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCloudEvents();

app.UseSwagger();
app.UseSwaggerUI();

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
    logger.LogInformation("sub: entering " + DateTime.UtcNow.ToString("hh:mm:ss.fff tt"));

    await Task.Delay(10000); // simulate work and make sure to avoid this is short

    logger.LogInformation("sub: leaving " + DateTime.UtcNow.ToString("hh:mm:ss.fff tt"));

    // return Results.NotFound(); // for "delete message"
    // return Results.UnprocessableEntity() // for "retry message"
    return Results.Ok(); // for "message processed"
});

app.MapPost("enqueue", async ([FromServices] DaprClient daprClient, [FromServices] ILogger<Program> logger, [FromBody] DemoEndpointRequest request) =>
{
    logger.LogInformation("enqueue: entering " + DateTime.UtcNow.ToString("hh:mm:ss.fff tt"));

    await daprClient.PublishEventAsync("demopubsub", "demotopic", new DemoPubSubMessage(request.Message));

    logger.LogInformation("enqueue: after PublishEventAsync " + DateTime.UtcNow.ToString("hh:mm:ss.fff tt"));

    return Results.Ok();
});

app.Run();

public record DemoPubSubMessage(string Message);
public record DemoEndpointRequest(string Message);