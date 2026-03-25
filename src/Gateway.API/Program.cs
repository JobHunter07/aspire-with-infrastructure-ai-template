using Gateway.API.Account;
using Gateway.API.Config;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");
builder.AddRedisClientBuilder("cache").WithOutputCache();
builder.Services.AddProblemDetails();

// https://medium.com/@ahmedmohamedelahmar/implement-api-gateway-with-token-handler-pattern-using-net-redis-and-keycloak-38250bfbd733
// https://github.com/Redestros/ApiGateway

builder.Services.AddHttpContextAccessor();
builder.Services.AddReverseProxy(builder.Configuration);
builder.Services.AddOAuthProxy();
builder.Services.AddAuthorizationPolicies();

const string corsPolicy = "defaultCorsPolicy";
builder.Services.AddCors(options => options.AddPolicy(corsPolicy,
    configurePolicy => configurePolicy                                                           
        .WithOrigins("http://localhost:4200", "https://localhost:7285", "http://localhost:8080", "https://localhost:54955") //ToDo:kbdavis07: Use Env var's for this
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseOutputCache();
app.UseHttpsRedirection();
app.MapDefaultEndpoints();
app.UseRouting();
app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapAccountEndpoints();
app.Run();
