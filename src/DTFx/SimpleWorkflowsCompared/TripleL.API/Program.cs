using DurableTask.Core;
using DurableTask.Samples.Greetings;
using WorkflowSamples;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<SampleService>();

builder.Services.AddDurableTaskAzureStorage(options =>
{
    options.TaskHubName = "TestDTfx";
    options.StorageConnectionString = "UseDevelopmentStorage=true;"; // default: Azurite
});

builder.Services.AddDurableTaskWorker(builder =>
{
    // Add orchestration with default name and version
    builder.AddOrchestration<GreetingsOrchestration>();
    builder.AddActivity<GetUserTask>();
    builder.AddActivity<SendGreetingTask>();

    // See https://github.com/lucaslorentz/durabletask-extensions/tree/main/src/LLL.DurableTask.Worker
    builder.HasAllOrchestrations = true;
    builder.HasAllActivities = true;
});

builder.Services.AddDurableTaskClient();

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