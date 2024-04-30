using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();

var useManSidekick = builder.Configuration.GetSection("DaprSidekick").Exists();
if (useManSidekick)
{
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

app.MapPost("sub", async (
        [FromBody] DemoPubSubMessage pubSubMessage
        ) =>
{
    string breakpoint2 = pubSubMessage.Message;

    // return Results.NotFound(); // for "delete message"
    // return Results.UnprocessableEntity() // for "retry message"
    return Results.Ok(); // for "message processed"
});

app.MapPost("enqueue", async ([FromServices] DaprClient daprClient, [FromBody] DemoEndpointRequest request) =>
{
    string breakpoint1 = request.Message;

    await daprClient.PublishEventAsync("demopubsub", "demotopic", new DemoPubSubMessage(request.Message));

    string breakpoint3 = request.Message;
    return Results.Ok();
});

app.Run();

public record DemoPubSubMessage(string Message);
public record DemoEndpointRequest(string Message);