namespace Tests.Acceptance.Keycloak;

/// <summary>
/// Acceptance tests for spec: keycloak-realm-hardening
/// Verifies the realm-export.json matches every scenario in the spec.
/// These tests parse the real config file — no live stack required.
/// </summary>
public sealed class RealmHardeningTests
{
    private readonly JsonElement _realm;

    public RealmHardeningTests()
    {
        var json = File.ReadAllText(TestEnvironment.RealmExportPath);
        using var doc = JsonDocument.Parse(json);
        _realm = doc.RootElement.Clone();
    }

    // --- Requirement: Brute force protection is enabled ---

    [Fact]
    public void BruteForceProtected_IsTrue()
        => Assert.True(_realm.GetProperty("bruteForceProtected").GetBoolean(),
            "bruteForceProtected must be true to defend against credential stuffing.");

    [Fact]
    public void PermanentLockout_IsFalse()
        => Assert.False(_realm.GetProperty("permanentLockout").GetBoolean(),
            "permanentLockout must be false so accounts are not permanently locked on first breach.");

    [Fact]
    public void FailureFactor_Is10()
        => Assert.Equal(10, _realm.GetProperty("failureFactor").GetInt32());

    [Fact]
    public void WaitIncrementSeconds_Is60()
        => Assert.Equal(60, _realm.GetProperty("waitIncrementSeconds").GetInt32());

    [Fact]
    public void MaxFailureWaitSeconds_Is900()
        => Assert.Equal(900, _realm.GetProperty("maxFailureWaitSeconds").GetInt32());

    // --- Requirement: Refresh token rotation is enabled ---

    [Fact]
    public void RevokeRefreshToken_IsTrue()
        => Assert.True(_realm.GetProperty("revokeRefreshToken").GetBoolean(),
            "revokeRefreshToken must be true to prevent replay attacks on refresh tokens.");

    // --- Requirement: TLS is required for all Keycloak connections ---

    [Fact]
    public void SslRequired_IsAll()
        => Assert.Equal("all", _realm.GetProperty("sslRequired").GetString(), StringComparer.Ordinal);
}
