using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace LivestockManager.UnitTests.Final;

public class RepositoryHygieneTests
{
    private readonly ITestOutputHelper _output;

    public RepositoryHygieneTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly string[] ForbiddenSuffixes = new[]
    {
        ".zip",
        ".bak",
        ".trx"
    };

    private static readonly string[] ForbiddenPathSegments = new[]
    {
        "/bin/",
        "\\bin\\",
        "/obj/",
        "\\obj\\",
        "/TestResults/",
        "\\TestResults\\",
        "/artifacts/e2e/.playwright-browsers/",
        "\\artifacts\\e2e\\.playwright-browsers\\"
    };

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !dir.GetFiles("LivestockManager.sln").Any())
        {
            dir = dir.Parent;
        }
        if (dir == null)
        {
            throw new DirectoryNotFoundException("Could not locate repository root (LivestockManager.sln not found in any ancestor of CWD).");
        }
        return dir.FullName;
    }

    private static List<string> GetGitLsFiles(string repoRoot)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "-C \"" + repoRoot + "\" ls-files",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("git process failed to start");
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            var err = proc.StandardError.ReadToEnd();
            throw new InvalidOperationException($"git ls-files exited {proc.ExitCode}: {err}");
        }
        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    [Fact]
    public void R1_NoTrackedFilesMatchForbiddenHygienePatterns()
    {
        var repoRoot = GetRepoRoot();
        _output.WriteLine("Repository root: " + repoRoot);
        var tracked = GetGitLsFiles(repoRoot);
        _output.WriteLine($"Tracked file count via git ls-files: {tracked.Count}");

        var violations = new List<string>();

        foreach (var rel in tracked)
        {
            var normalized = rel.Replace('\\', '/');

            if (normalized.Equals("audit.zip", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(rel + " [specific: audit.zip must never be tracked]");
            }

            var ext = Path.GetExtension(rel);
            if (!string.IsNullOrEmpty(ext) && ForbiddenSuffixes.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                bool allowedTtf =
                    normalized.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                    && normalized.StartsWith("src/LivestockManager.Infrastructure/Resources/Fonts/", StringComparison.OrdinalIgnoreCase);
                bool allowedOfl =
                    normalized.EndsWith("OFL.txt", StringComparison.OrdinalIgnoreCase)
                    && normalized.StartsWith("src/LivestockManager.Infrastructure/Resources/Fonts/", StringComparison.OrdinalIgnoreCase);

                if (!allowedTtf && !allowedOfl)
                {
                    violations.Add(rel + " [extension " + ext + "]");
                }
            }

            foreach (var seg in ForbiddenPathSegments)
            {
                var nSeg = seg.Replace('\\', '/');
                if (normalized.Contains(nSeg, StringComparison.Ordinal))
                {
                    violations.Add(rel + " [segment " + nSeg + "]");
                    break;
                }
            }
        }

        foreach (var v in violations)
        {
            _output.WriteLine("VIOLATION: " + v);
        }

        Assert.Empty(violations);
    }
}
