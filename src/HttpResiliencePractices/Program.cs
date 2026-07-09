using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;

// Based on https://milanjovanovic.tech/blog/overriding-default-http-resilience-handlers-in-dotnet
// with https://github.com/dotnet/extensions/pull/5801 added (see csproj for NoWarn)

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddHttpClient()
    .ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

builder.Services
    .AddHttpClient("github")
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://api.github.com");
    })
    .RemoveAllResilienceHandlers()
    .AddResilienceHandler("custom", pipeline =>
    {
        pipeline.AddTimeout(TimeSpan.FromSeconds(10));

        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(500)
        });

        pipeline.AddTimeout(TimeSpan.FromSeconds(1));
    });


using IHost host = builder.Build();
await host.RunAsync();