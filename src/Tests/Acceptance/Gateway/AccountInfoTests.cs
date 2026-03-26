namespace Tests.Acceptance.Gateway;

/// <summary>
/// Acceptance tests for spec: gateway-auth-hardening — account info claims allowlist.
/// Requires the Aspire stack to be running.
/// </summary>
public sealed class AccountInfoTests
{
    // --- Requirement: Account info endpoint returns only required fields ---

    [Fact]
    public async Task GetAccountInfo_Unauthenticated_DoesNotReturn200()
    {
        using var client = TestEnvironment.CreateHttpClient(allowRedirects: false);

        var response = await client.GetAsync(
            $"{TestEnvironment.GatewayBaseUrl}/api/account/info");

        // Must never hand back data to an unauthenticated caller
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountInfo_Unauthenticated_ResponseBodyDoesNotContainRawClaims()
    {
        using var client = TestEnvironment.CreateHttpClient(allowRedirects: false);

        var response = await client.GetAsync(
            $"{TestEnvironment.GatewayBaseUrl}/api/account/info");

        var body = await response.Content.ReadAsStringAsync();

        // The old implementation returned {"Type":"...","Value":"..."} for every claim.
        // The new implementation returns {"username":...,"email":...,"roles":...}.
        // Neither "Type" nor "Value" (capital T/V) should appear at any path.
        Assert.DoesNotContain("\"Type\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Value\"", body, StringComparison.Ordinal);
    }
}
