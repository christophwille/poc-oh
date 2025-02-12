using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

var loggerFactory = LoggerFactory.Create(i =>
{
    i.AddConfiguration(builder.Configuration.GetSection("Logging"));
    i.AddConsole();
});
var logger = loggerFactory.CreateLogger<Program>();

builder.ConfigureFunctionsWebApplication();

//// https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.OpenTelemetry/#readme-body-tab
//builder.Services.AddOpenTelemetry()
// .UseFunctionsWorkerDefaults();
// .UseAzureMonitor();

builder.Build().Run();


// This hopefully will go away in .NET 10...
public partial class Program { }