using DurableTask.Core;
using DurableTask.DependencyInjection;
using DurableTask.Emulator;
using DurableTask.Hosting;
using DurableTask.Samples.Greetings;
using WorkflowSamples;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<SampleService>();

builder.Services.AddSingleton<GreetingsOrchestration>();

// https://github.com/jviau/durabletask-hosting
// TODO: Fix up with more from https://github.com/jviau/durabletask-hosting/blob/5d2328d295c3030f1870fdef10328270fcba502a/samples/DurableTask.Samples/Program.cs
builder.Host.ConfigureTaskHubWorker((context, builder) =>
{
    //// add orchestration service
    builder.WithOrchestrationService(new LocalOrchestrationService());

    //// add orchestration directly _not_ in service container. Will be treated as transient.
    //builder.AddOrchestration<MyTransientOrchestration>();

    //// will be fetched from service provider.
    //builder.AddOrchestration<MySingletonOrchestration>();

    //// will be fetched from service provider.
    //builder.UseOrchestrationMiddleware<MyScopedMiddleware>();

    //// same as orchestration: can be part of the services or not.
    //builder.AddActivity<MyTransientActivity>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/start", async (TaskHubClient taskHubClient) =>
{
    string instanceId = Guid.NewGuid().ToString();
    var instance = await taskHubClient.CreateOrchestrationInstanceAsync(typeof(GreetingsOrchestration), instanceId, null);

    return instance.InstanceId;
})
.WithName("StartDemoWorkflow")
.WithOpenApi();

app.Run();