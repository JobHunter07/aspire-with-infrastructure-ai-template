using GatewayHost.Extensions;
using GatewayHost.Modules.Api.Features.BookFeature;
using GatewayHost.Modules.Api.Features.Weather;
using GatewayHost.Modules.Bff;
using System.Reflection;

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

builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddGatewayYarp();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapWeatherEndpoint();
app.MapCreateBookEndpoint();
app.MapBffEndpoints();
app.MapGatewayAuthEndpoints();
app.MapReverseProxy();

app.Run();

