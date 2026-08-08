using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace LivestockManager.EndToEndTests;

/// <summary>
/// External dependencies required for E2E tests.
///   - Playwright binaries installed via Microsoft.Playwright build targets.
///   - Chromium browser installed via `playwright.ps1 install chromium` (or
///     node playwright.CLI.js install chromium).
///   - E2E_BASE_URL set by the orchestrator (scripts/Run-E2ETests.ps1) to
///     point at the running ASP.NET Core web host for this test run.
/// If any of these are missing the test is NOT marked with an unconditional
/// [Fact(Skip = "...")]. Instead, it dynamically records a skip outcome with
/// a clearly documented external blocker so CI can distinguish
/// "unexpected skip" (counts as fail per runner exit rules) from
/// "blocked_external skip, documented".
/// </summary>
public static class E2ETestEnvironment
{
    public const string BlockerMissingBaseUrl =
        "blocked_external: E2E_BASE_URL environment variable is not set. " +
        "Use scripts/Run-E2ETests.ps1 or run-e2e-tests.cmd to start the web application and export E2E_BASE_URL.";

    public const string BlockerPlaywrightOrChromiumMissing =
        "blocked_external: Playwright managed assembly, native playwright CLI, or Chromium browser is unavailable. " +
        "Run scripts/Run-E2ETests.ps1 (default). It detects missing Chromium and installs ONLY Chromium when absent. " +
        "Manual fallback: build tests/LivestockManager.EndToEndTests then invoke the generated playwright.ps1/playwright.CLI.js with 'install chromium'.";

    private static readonly object _lock = new();
    private static bool _evaluated;
    private static string? _blocker;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static bool _headed;

    public static string BaseUrl
    {
        get
        {
            var v = Environment.GetEnvironmentVariable("E2E_BASE_URL");
            if (!string.IsNullOrWhiteSpace(v)) return v.TrimEnd('/');
            return string.Empty;
        }
    }

    public static bool IsHeaded
    {
        get
        {
            var s = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED") ??
                    Environment.GetEnvironmentVariable("HEADED");
            return s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static string ScreenshotDir => SafeDir("E2E_SCREENSHOT_DIR", "artifacts/e2e/screenshots");
    public static string TraceDir => SafeDir("E2E_TRACE_DIR", "artifacts/e2e/traces");
    public static string VideoDir => SafeDir("E2E_VIDEO_DIR", "artifacts/e2e/videos");
    public static string LogDir => SafeDir("E2E_LOG_DIR", "artifacts/e2e/logs");

    public static string? DetectBlocker()
    {
        lock (_lock)
        {
            if (_evaluated) return _blocker;
            _evaluated = true;

            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                _blocker = BlockerMissingBaseUrl;
                return _blocker;
            }

            try
            {
                _headed = IsHeaded;
                _playwright = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
                if (_playwright == null)
                {
                    _blocker = BlockerPlaywrightOrChromiumMissing;
                    return _blocker;
                }
                _browser = _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = !_headed,
                    Timeout = 60_000f,
                    Args = new[]
                    {
                        "--disable-dev-shm-usage",
                        "--no-sandbox",
                        "--disable-gpu"
                    }
                }).GetAwaiter().GetResult();
                if (_browser == null || _browser.IsConnected == false)
                {
                    _blocker = BlockerPlaywrightOrChromiumMissing;
                    return _blocker;
                }
            }
            catch (Exception ex)
            {
                _blocker = BlockerPlaywrightOrChromiumMissing + " Exception: " + ex.Message;
                return _blocker;
            }

            _blocker = null;
            return null;
        }
    }

    public static IPlaywright Playwright
    {
        get
        {
            if (_playwright == null) throw new InvalidOperationException("Playwright not initialized; call DetectBlocker first.");
            return _playwright;
        }
    }

    public static IBrowser Browser
    {
        get
        {
            if (_browser == null) throw new InvalidOperationException("Browser not initialized; call DetectBlocker first.");
            return _browser;
        }
    }

    public static bool VideosEnabled => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("E2E_ENABLE_VIDEO"))
                                        || Directory.Exists(VideoDir);

    public static async ValueTask CleanupAsync()
    {
        try
        {
            if (_browser != null)
            {
                try { await _browser.CloseAsync(); } catch { }
                await _browser.DisposeAsync();
                _browser = null;
            }
            _playwright?.Dispose();
            _playwright = null;
        }
        catch
        {
        }
        finally
        {
            _evaluated = false;
            _blocker = null;
        }
    }

    public static void EnsureArtifactsDirs()
    {
        _ = ScreenshotDir; _ = TraceDir; _ = VideoDir; _ = LogDir;
    }

    private static string SafeDir(string envName, string relDefault)
    {
        var v = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(v))
        {
            try { if (!Directory.Exists(v)) Directory.CreateDirectory(v); } catch { }
            return v;
        }
        var abs = Path.Combine(FindRepoRoot(), relDefault);
        try { if (!Directory.Exists(abs)) Directory.CreateDirectory(abs); } catch { }
        return abs;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 16; i++)
        {
            if (File.Exists(Path.Combine(dir, "LivestockManager.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrWhiteSpace(dir)) return Environment.CurrentDirectory;
        }
        return Environment.CurrentDirectory;
    }

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static void WriteLog(string category, string message)
    {
        var stamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        var line = $"[{stamp}] [{category}] {message}{Environment.NewLine}";
        try { File.AppendAllText(Path.Combine(LogDir, "e2e-tests.log"), line); } catch { }
        Debug.Write(line);
    }
}
