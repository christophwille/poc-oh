// see: https://nodatime.org/3.3.x/userguide/
// see also: https://dev.to/bwi/net-in-practice-modeling-time-with-nodatime-o6d

using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.TryAddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/time", (IClock nodaClock) =>
{
    // utc time
    DateTime utcNow = nodaClock.GetCurrentInstant().ToDateTimeUtc();

    // Current time in a specific time zone
    var tz = DateTimeZoneProviders.Tzdb["Europe/Berlin"];
    var instant = nodaClock.GetCurrentInstant();
    var zonedDateTime = instant.InZone(tz);
    DateTime currentTimeAdjusted = zonedDateTime.ToDateTimeUnspecified();

    // to iso string
    var value = currentTimeAdjusted;
    var dateTime = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    var dateTimeUtc = LocalDateTime.FromDateTime(dateTime).InZoneLeniently(tz).ToInstant().InZone(tz).ToDateTimeUtc();
    string isoString = dateTimeUtc.ToString("o");

    return new TimeResponse(utcNow, currentTimeAdjusted, isoString);
})
.WithName("GetTime");

app.Run();

static DateTime ParseIsoDateTime(string value)
{
    var dt = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    return dt.Kind == DateTimeKind.Utc ?
        Instant.FromDateTimeUtc(dt).InZone(DateTimeZoneProviders.Tzdb["Europe/Berlin"]).ToDateTimeUnspecified() :
        dt;
}

internal record TimeResponse(DateTime UtcNow, DateTime CurrentTimeAdjusted, string CurrentTimeAdjustedIso);

