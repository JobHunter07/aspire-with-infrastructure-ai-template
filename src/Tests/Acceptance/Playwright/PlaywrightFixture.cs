using Microsoft.Playwright;

namespace Tests.Acceptance.Playwright;

/// <summary>
/// Shared Playwright fixture — one browser instance per test class.
/// Collects per-test results and writes the markdown report on dispose.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright _playwright = null!;

    public IBrowser Browser { get; private set; } = null!;

    /// <summary>Accumulated results written to the report in DisposeAsync.</summary>
    public List<ScreenshotResult> Results { get; } = [];

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(TestEnvironment.ScreenshotsDir);
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
        _playwright.Dispose();
        await TestReportWriter.WriteAsync(TestEnvironment.ReportPath, Results);
    }
}
