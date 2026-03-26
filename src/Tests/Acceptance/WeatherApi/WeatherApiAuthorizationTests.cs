namespace Tests.Acceptance.WeatherApi;

/// <summary>
/// Acceptance tests for spec: weather-api-authorization
/// Hits the real Weather.API — requires the Aspire stack to be running.
/// </summary>
public sealed class WeatherApiAuthorizationTests
{
    // --- Requirement: Weather forecast endpoint requires authorization ---

    [Fact]
    public async Task GetWeatherForecast_WithoutToken_Returns401()
    {
        using var client = TestEnvironment.CreateHttpClient();

        var response = await client.GetAsync(
            $"{TestEnvironment.WeatherApiBaseUrl}/api/weatherforecast");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWeatherForecast_WithInvalidToken_Returns401()
    {
        using var client = TestEnvironment.CreateHttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        var response = await client.GetAsync(
            $"{TestEnvironment.WeatherApiBaseUrl}/api/weatherforecast");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWeatherForecast_DirectAccess_WithoutToken_Returns401()
    {
        // Bypasses YARP gateway entirely — hits the Weather.API port directly.
        // Proves authorization is enforced at the service level, not only at the gateway.
        using var client = TestEnvironment.CreateHttpClient();

        var response = await client.GetAsync(
            $"{TestEnvironment.WeatherApiBaseUrl}/api/weatherforecast");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
