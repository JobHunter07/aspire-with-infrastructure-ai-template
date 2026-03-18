using GatewayHost.Modules.Api.Features.BookFeature;
using GatewayHost.Modules.Bff;
using Scalar.AspNetCore;
using System.Reflection;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Load developer user-secrets (if present) so sensitive Keycloak client settings
// like ClientId/ClientSecret can be provided via `dotnet user-secrets` during local dev.
if (builder.Environment.IsDevelopment())
{
    // optional: true so app still runs when no user-secrets are configured
    builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
}

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddRedisClientBuilder("cache").WithOutputCache();
builder.AddRedisDistributedCache(connectionName: "cache");

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.Services.RegisterApiEndpointsFromAssembly(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseOutputCache();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

var api = app.MapGroup("/api");
api.MapGet("weatherforecast", () =>
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
.CacheOutput(p => p.Expire(TimeSpan.FromSeconds(5)))
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

// Map sample Minimal API endpoint(s)
app.MapCreateBookEndpoint();

// Map BFF endpoints (user/session login flow)
app.MapBffEndpoints();

app.UseFileServer();

app.MapScalarApiReference(options =>
{
    options.WithTheme(ScalarTheme.DeepSpace);

    // ?? THIS enables Developer Tools (request + client code panel)
    options.WithDefaultHttpClient(
        ScalarTarget.CSharp,
        ScalarClient.HttpClient);

});


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
