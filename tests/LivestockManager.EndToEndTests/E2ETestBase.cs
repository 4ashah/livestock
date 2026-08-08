using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace LivestockManager.EndToEndTests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
internal sealed class ClearSynchronizationContextAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
    }

    public override void After(MethodInfo methodUnderTest)
    {
    }
}

internal static class BlockerSkip
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void If(bool condition, string reason)
    {
        if (!condition) return;
        var ctor = typeof(SkipException).GetConstructors(
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        Exception? ex = null;
        foreach (var c in ctor)
        {
            var ps = c.GetParameters();
            if (ps.Length == 2 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(string))
            {
                ex = (Exception)c.Invoke(new object[] { reason, null! });
                break;
            }
            if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
            {
                ex = (Exception)c.Invoke(new object[] { reason });
                break;
            }
            if (ps.Length == 0)
            {
                ex = (Exception)c.Invoke(Array.Empty<object>());
                break;
            }
        }
        ex ??= new InvalidOperationException("blocked_external: " + reason);
        throw ex;
    }
}

/// <summary>
/// Base class for all Playwright-based E2E tests. Provides:
///   - Base URL resolved from E2E_BASE_URL (set by scripts/Run-E2ETests.ps1).
///   - One shared IBrowser per test class (via static init; disposed via
///     DisposeAsync at the end of the test run by E2ETestEnvironment.CleanupAsync
///     called from the assembly fixture).
///   - New IBrowserContext per test. Screenshots on failure. Traces on failure
///     when the framework supports it (Playwright Tracing API is used).
///   - Videos when explicitly enabled via E2E_ENABLE_VIDEO=1 (they are heavy).
///   - Clearly-documented dynamic skip when external dependencies are missing
///     (see E2ETestEnvironment.DetectBlocker).
/// IMPORTANT: tests must NOT use unconditional [Fact(Skip = "...")]. They
/// should use Skip.If(... documented reason ...) inside the test body so
/// that Xunit records a skipped result only when the documented external
/// dependency is truly unavailable.
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IPage? _page;
    private IBrowserContext? _context;
    private bool _disposed;
    private static int _browserCleanupRegistered;

    protected E2ETestBase(ITestOutputHelper output)
    {
        _output = output;
        E2ETestEnvironment.EnsureArtifactsDirs();
        EnsureAssemblyCleanupRegistered();
    }

    private static void EnsureAssemblyCleanupRegistered()
    {
        if (Interlocked.CompareExchange(ref _browserCleanupRegistered, 1, 0) != 0) return;
        AppDomain.CurrentDomain.ProcessExit += (_, __) =>
        {
            try { E2ETestEnvironment.CleanupAsync().GetAwaiter().GetResult(); } catch { }
        };
        AppDomain.CurrentDomain.DomainUnload += (_, __) =>
        {
            try { E2ETestEnvironment.CleanupAsync().GetAwaiter().GetResult(); } catch { }
        };
    }

    public string BaseUrl => E2ETestEnvironment.BaseUrl;
    protected ITestOutputHelper Output => _output;

    protected async Task<IPage> NewPageAsync(string testId, BrowserNewContextOptions? overrides = null)
    {
        var blocker = E2ETestEnvironment.DetectBlocker();
        if (!string.IsNullOrEmpty(blocker)) BlockerSkip.If(true, blocker);

        var videosDir = E2ETestEnvironment.VideosEnabled ? E2ETestEnvironment.VideoDir : null;
        var traceFile = Path.Combine(E2ETestEnvironment.TraceDir, $"{Sanitize(testId)}.zip");

        var ctxOptions = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true,
            BaseURL = BaseUrl,
            RecordVideoDir = !string.IsNullOrEmpty(videosDir) ? videosDir : null,
            RecordVideoSize = !string.IsNullOrEmpty(videosDir) ? new RecordVideoSize { Width = 1280, Height = 720 } : null,
            Locale = "en-US",
            TimezoneId = "UTC"
        };
        if (overrides != null)
        {
            if (overrides.ViewportSize != null) ctxOptions.ViewportSize = overrides.ViewportSize;
            if (!string.IsNullOrEmpty(overrides.Locale)) ctxOptions.Locale = overrides.Locale;
            if (!string.IsNullOrEmpty(overrides.TimezoneId)) ctxOptions.TimezoneId = overrides.TimezoneId;
        }

        _context = await E2ETestEnvironment.Browser.NewContextAsync(ctxOptions);
        try
        {
            await _context.Tracing.StartAsync(new TracingStartOptions
            {
                Title = testId,
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }
        catch
        {
            // Tracing unavailable (e.g., older playwright nupkg). Continue without it.
        }

        _page = await _context.NewPageAsync();
        return _page;
    }

    protected internal async Task CaptureFailureArtifactsIfAny(string testId, Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var name = $"{Sanitize(testId)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            if (_page != null)
            {
                var shot = Path.Combine(E2ETestEnvironment.ScreenshotDir, $"{name}.png");
                try { await _page.ScreenshotAsync(new PageScreenshotOptions { Path = shot, FullPage = true, Type = ScreenshotType.Png }); }
                catch { /* swallow — capture is best-effort */ }

                try { _output?.WriteLine($"Screenshot captured: {shot}"); } catch { }
                try { E2ETestEnvironment.WriteLog("FAIL", $"{testId} -> {shot} :: {ex.Message}"); } catch { }
            }
            if (_context != null)
            {
                var tracePath = Path.Combine(E2ETestEnvironment.TraceDir, $"{name}.zip");
                try
                {
                    await _context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
                }
                catch
                {
                    // Tracing not supported / StartAsync was skipped.
                }
            }
        }
        catch
        {
            // capture must never propagate
        }
    }

    protected internal async Task CloseContextAsync()
    {
        try
        {
            if (_context != null)
            {
                // Videos are finalized at CloseAsync time. Skip trace stop unless failure already did.
                try { await _context.Tracing.StopAsync(); } catch { }
                await _context.CloseAsync();
                await _context.DisposeAsync();
            }
        }
        catch
        {
        }
        finally
        {
            _page = null;
            _context = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await CloseContextAsync(); } catch { }
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    Task IAsyncLifetime.DisposeAsync() => DisposeAsync();

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "unnamed";
        var arr = input.ToCharArray();
        for (int i = 0; i < arr.Length; i++)
        {
            var c = arr[i];
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.') continue;
            arr[i] = '_';
        }
        return new string(arr);
    }

    /// <summary>
    /// Xunit v2 ITestOutputHelper-aware fact runner. Called from each [Fact]
    /// wrapper so we can capture failure artifacts and still let the test
    /// report pass/fail/skip to the runner naturally.
    /// </summary>
    protected async Task RunAsync(string testId, Func<Task> body)
    {
        Exception? ex = null;
        try
        {
            await body();
        }
        catch (SkipException)
        {
            // Documented skip: propagate.
            throw;
        }
        catch (Exception e)
        {
            ex = e;
            throw;
        }
        finally
        {
            try { await CaptureFailureArtifactsIfAny(testId, ex); } catch { }
        }
    }
}

/// <summary>
/// Assembly-level fixture to guarantee that browser resources are released
/// after all E2E tests have completed, regardless of whether individual
/// tests pass / fail / skip.
/// </summary>
public sealed class E2ETestAssemblyFixture : IDisposable
{
    public E2ETestAssemblyFixture()
    {
    }

    public void Dispose()
    {
        try { E2ETestEnvironment.CleanupAsync().GetAwaiter().GetResult(); } catch { }
    }
}

[CollectionDefinition(nameof(E2ETestCollection))]
public sealed class E2ETestCollection : ICollectionFixture<E2ETestAssemblyFixture>
{
}

[Collection(nameof(E2ETestCollection))]
public abstract class E2ETestCollectionBase : E2ETestBase
{
    // ReSharper disable once UnusedParameter.Global
    protected E2ETestCollectionBase(ITestOutputHelper output, E2ETestAssemblyFixture _) : base(output)
    {
    }
}
