namespace Tests.Acceptance.Gateway;

/// <summary>
/// Acceptance tests for spec: gateway-auth-hardening — logout CSRF protection.
/// Requires the Aspire stack to be running.
/// </summary>
public sealed class LogoutCsrfTests
{
    // --- Requirement: Logout endpoint accepts only POST requests ---

    [Fact]
    public async Task GetLogout_Returns405MethodNotAllowed()
    {
        using var client = TestEnvironment.CreateHttpClient();

        var response = await client.GetAsync(
            $"{TestEnvironment.GatewayBaseUrl}/api/account/logout");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task PostLogout_Unauthenticated_IsNotMethodNotAllowed()
    {
        // POST is the accepted verb — unauthenticated users get a challenge (302/401),
        // never a 405. This proves the CSRF-safe endpoint is correctly registered.
        using var client = TestEnvironment.CreateHttpClient(allowRedirects: false);

        using var response = await client.PostAsync(
            $"{TestEnvironment.GatewayBaseUrl}/api/account/logout", content: null);

        Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
