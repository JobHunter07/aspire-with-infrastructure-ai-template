namespace Tests.Acceptance.Playwright;

internal static class TestReportWriter
{
    public static async Task WriteAsync(string reportPath, IReadOnlyList<ScreenshotResult> results)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

        var passed = results.Count(r => r.Passed);
        var failed = results.Count(r => !r.Passed);
        var overallStatus = failed == 0 ? "✅ ALL PASS" : $"❌ {failed} FAILED";
        var runDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

        var sb = new StringBuilder();

        sb.AppendLine("# Security Acceptance Test Report");
        sb.AppendLine();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| **Run Date** | {runDate} |");
        sb.AppendLine($"| **Branch** | security/Implement-2026-03-25-security-review-changes |");
        sb.AppendLine($"| **Change** | implement-security-review-2026-03-25 |");
        sb.AppendLine($"| **Status** | {overallStatus} |");
        sb.AppendLine($"| **Tests** | {results.Count} total · {passed} passed · {failed} failed |");
        sb.AppendLine();
        sb.AppendLine("> All screenshots were captured against the **live Aspire stack** (`aspire run`).");
        sb.AppendLine("> No mocks — Dev is Prod.");
        sb.AppendLine();

        sb.AppendLine("## Results Summary");
        sb.AppendLine();
        sb.AppendLine("| Status | Spec | Scenario | Notes |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var r in results)
        {
            var icon = r.Passed ? "✅" : "❌";
            sb.AppendLine($"| {icon} | {r.Spec} | {r.Scenario} | {r.Notes} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Screenshot Evidence");
        sb.AppendLine();

        foreach (var r in results)
        {
            var relativeScreenshot = "./screenshots/" + Path.GetFileName(r.ScreenshotPath);
            var icon = r.Passed ? "✅" : "❌";

            sb.AppendLine($"### {icon} {r.TestName}");
            sb.AppendLine();
            sb.AppendLine($"**Spec:** {r.Spec}  ");
            sb.AppendLine($"**Scenario:** {r.Scenario}  ");
            if (!string.IsNullOrWhiteSpace(r.Notes))
                sb.AppendLine($"**Notes:** {r.Notes}  ");
            sb.AppendLine();

            if (File.Exists(r.ScreenshotPath))
                sb.AppendLine($"![{r.TestName}]({relativeScreenshot})");
            else
                sb.AppendLine("_Screenshot not captured._");

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(reportPath, sb.ToString());
    }
}
