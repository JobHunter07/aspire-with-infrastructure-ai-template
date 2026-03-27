namespace Tests.Acceptance.Playwright;

public sealed record ScreenshotResult(
    string TestName,
    string Spec,
    string Scenario,
    bool Passed,
    string ScreenshotPath,
    string Notes = "");
