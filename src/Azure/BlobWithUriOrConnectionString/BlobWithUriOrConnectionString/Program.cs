using Azure.Identity;
using BlobWithUriOrConnectionString;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// NOTE: if connection string contains only storage account name => URI based with managed identity
builder.Services.AddAzureClients(clientBuilder =>
{
    const string BlobUri = "https://{0}.blob.core.windows.net";
    const string StorageClientName = "default";

    string? blogStg = builder.Configuration.GetConnectionString("Demo");

    var storageSettings = StorageSettings.Create(blogStg!);
    // Should be actually registered in the DI container as options properly
    // builder.Services.AddSingleton(storageSettings);

    // https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection 
    if (storageSettings.IsUriBased)
    {
        clientBuilder
            .AddBlobServiceClient(new Uri(String.Format(BlobUri, blogStg)))
            .WithName(StorageClientName);
    }
    else
    {
        clientBuilder
            .AddBlobServiceClient(blogStg)
            .WithName(StorageClientName);
    }

    // Basic options https://blog.jongallant.com/2021/08/azure-identity-201/
    // Optimize via https://scottsauber.com/2022/05/10/improving-azure-key-vault-performance-in-asp-net-core-by-up-to-10x/
    // Ordered resolution via ChainedTokenCredential: https://yourazurecoach.com/2020/08/13/managed-identity-simplified-with-the-new-azure-net-sdks/
    Azure.Core.TokenCredential? credential = null;
    if (builder.Environment.IsDevelopment())
    {
        credential = new VisualStudioCredential();
    }
    else
    {
        credential = new ChainedTokenCredential(
                new ManagedIdentityCredential(),
                new VisualStudioCredential(),
                new AzurePowerShellCredential()
            );
    }

    clientBuilder.UseCredential(credential);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

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
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
