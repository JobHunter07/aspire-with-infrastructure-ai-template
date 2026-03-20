namespace GatewayHost.Modules.Api.Features.Weather;

/// <summary>
/// Provides extension methods for mapping weather forecast endpoints to an endpoint route builder.
/// </summary>
/// <remarks>This class enables the registration of a weather forecast API endpoint within an ASP.NET Core
/// application. The endpoint returns a collection of weather forecast data and is configured with caching and tagging
/// for improved performance and discoverability.</remarks>
public static class WeatherEndpoint
{
    /// <summary>
    /// Maps the weather forecast endpoint to the specified endpoint route builder, enabling a GET API at
    /// '/api/weatherforecast' that returns a collection of weather forecasts.
    /// </summary>
    /// <remarks>The mapped endpoint supports output caching with a 5-second expiration and is tagged as
    /// 'Weather'. The endpoint returns a five-day weather forecast with randomized data for demonstration
    /// purposes.</remarks>
    /// <param name="endpoints">The endpoint route builder to which the weather forecast endpoint will be mapped.</param>
    /// <returns>The endpoint route builder with the weather forecast endpoint added.</returns>
    public static IEndpointRouteBuilder MapWeatherEndpoint(this IEndpointRouteBuilder endpoints)
    {
        string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        endpoints.MapGet("/api/weatherforecast", () =>
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
        .WithName("GetWeatherForecast")
        .WithTags("Weather");

        return endpoints;
    }

    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }

}
