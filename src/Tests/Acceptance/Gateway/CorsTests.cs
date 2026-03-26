namespace Tests.Acceptance.Gateway;

/// <summary>
/// Acceptance tests for spec: gateway-auth-hardening — CORS policy.
/// Requires the Aspire stack to be running.
/// </summary>
public sealed class CorsTests
{
    private const string AllowedOrigin = TestEnvironment.FrontendBaseUrl;

    // --- Requirement: CORS policy is restricted to an explicit method and header allowlist ---

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("OPTIONS")]
    public async Task CorsPreflight_AllowedMethod_IsIncludedInAllowMethods(string method)
    {
        using var client = TestEnvironment.CreateHttpClient();
        var request = new HttpRequestMessage(
            HttpMethod.Options,
            $"{TestEnvironment.GatewayBaseUrl}/api/account/public");

        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        var response = await client.SendAsync(request);

        var allowedMethods = GetHeader(response, "Access-Control-Allow-Methods");
        Assert.Contains(method, allowedMethods, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("DELETE")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task CorsPreflight_DisallowedMethod_IsNotIncludedInAllowMethods(string method)
    {
        using var client = TestEnvironment.CreateHttpClient();
        var request = new HttpRequestMessage(
            HttpMethod.Options,
            $"{TestEnvironment.GatewayBaseUrl}/api/account/public");

        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", method);

        var response = await client.SendAsync(request);

        var allowedMethods = GetHeader(response, "Access-Control-Allow-Methods");
        Assert.DoesNotContain(method, allowedMethods, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorsPreflight_AllowedHeader_ContentType_IsAccepted()
    {
        using var client = TestEnvironment.CreateHttpClient();
        var request = new HttpRequestMessage(
            HttpMethod.Options,
            $"{TestEnvironment.GatewayBaseUrl}/api/account/public");

        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        var response = await client.SendAsync(request);

        var allowedHeaders = GetHeader(response, "Access-Control-Allow-Headers");
        Assert.Contains("Content-Type", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetHeader(HttpResponseMessage response, string header) =>
        response.Headers.TryGetValues(header, out var values)
            ? string.Join(", ", values)
            : string.Empty;
}
