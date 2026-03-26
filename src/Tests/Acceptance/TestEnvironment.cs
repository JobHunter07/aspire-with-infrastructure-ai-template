namespace Tests.Acceptance;

/// <summary>
/// Shared constants and HTTP client factory for acceptance tests.
/// All tests run against the real live Aspire stack — no mocks.
/// Start the stack with: aspire run
/// Install Playwright browsers once with: playwright install chromium
/// </summary>
internal static class TestEnvironment
{
    public const string GatewayBaseUrl = "https://localhost:7415";
    public const string WeatherApiBaseUrl = "https://localhost:7593";
    public const string FrontendBaseUrl = "https://localhost:54955";
    public const string AspireDashboardUrl = "https://localhost:17125";

    private static readonly Lazy<string> _workspaceRoot = new(ResolveWorkspaceRoot);

    public static string WorkspaceRoot => _workspaceRoot.Value;

    public static string RealmExportPath =>
        Path.Combine(WorkspaceRoot, "src", "AppHost", "Realms", "realm-export.json");

    public static string ScreenshotsDir =>
        Path.Combine(WorkspaceRoot, "docs", "test-reports", "screenshots");

    public static string ReportPath =>
        Path.Combine(WorkspaceRoot, "docs", "test-reports", "security-acceptance-report.md");

    /// <summary>
    /// Creates an HttpClient targeting the live Aspire stack.
    /// Dev certs are trusted; redirects are NOT followed by default so
    /// tests can assert on the raw status codes they receive.
    /// </summary>
    public static HttpClient CreateHttpClient(bool allowRedirects = false) =>
        new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            AllowAutoRedirect = allowRedirects
        });

    private static string ResolveWorkspaceRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetDirectories("openspec").Length > 0 || dir.GetDirectories(".git").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Cannot locate workspace root. Ensure the tests are run from within the repository.");
    }
}
