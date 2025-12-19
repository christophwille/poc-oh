using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AwsS3Setting>(builder.Configuration.GetSection(AwsS3Setting.Section));

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/s3test", async (IOptionsSnapshot<AwsS3Setting> snapShot) =>
{
    // Taken from: https://github.com/rustfs/rustfs-dotnet-demo
    ListObjectsV2Request request = new()
    {
        BucketName = "my-bucket"
    };

    var settings = snapShot.Value;

    var rustfs = builder.Configuration.GetConnectionString("rustfs");

    var config = new AmazonS3Config
    {
        ServiceURL = settings.ServiceUrl,
        ForcePathStyle = true,
        UseHttp = true
    };

    var _s3client = new AmazonS3Client(
       credentials: new BasicAWSCredentials(settings.AccessKey, settings.SecretKey),
       clientConfig: config);

    var response = await _s3client.ListObjectsV2Async(request);

    return response.S3Objects.Select(o => o.Key).ToList();
})
.WithName("GetFileList");

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

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

public sealed class AwsS3Setting
{
    public const string Section = "RustFS";
    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}