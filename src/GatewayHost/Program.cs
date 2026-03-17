using GatewayHost.Modules.Api.Features.BookFeature.CreateBook;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Map sample Minimal API endpoint(s)
app.MapCreateBookEndpoint();

app.Run();
