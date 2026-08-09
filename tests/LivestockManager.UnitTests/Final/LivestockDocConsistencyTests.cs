using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace LivestockManager.UnitTests.Final;

public class LivestockDocConsistencyTests
{
    private static readonly string[] ForbiddenStaleWords = new[]
    {
        "Aves",
        "Poultry",
        "Swine",
        "Pigs",
        "Piglet",
        "Adult Bovine",
        "Young Bovine",
        "Calf",
        "Calves",
        "Purchased chick",
        "Cow",
        "Bull",
    };

    private static readonly string[] LivestockCodes = new[] { "Ah", "Su", "Sa", "Ad", "Sd" };

    private static readonly HashSet<string> ExcludedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "PHASE1_SOURCE_INVENTORY.md"
    };

    private static readonly Regex HistoricalQuoteStripRegex = new Regex(
        @"<!--\s*HISTORICAL_BASELINE_QUOTE_START\s*-->.*?<!--\s*HISTORICAL_BASELINE_QUOTE_END\s*-->",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private static readonly Regex CodeAhWordBoundary = new Regex(@"\bAh\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CodeSuWordBoundary = new Regex(@"\bSu\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CodeSaWordBoundary = new Regex(@"\bSa\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CodeAdWordBoundary = new Regex(@"\bAd\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CodeSdWordBoundary = new Regex(@"\bSd\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string StripHistoricalQuoteRegions(string content)
    {
        return HistoricalQuoteStripRegex.Replace(content, " ");
    }

    [Fact]
    public void DoesNotAssociateAhSuSaAdSdWithStaleDomesticAnimalMeanings_DocsAndAudit()
    {
        string repoRoot = FindRepoRoot();
        Assert.True(Directory.Exists(repoRoot), $"Repo root not found (looked for .git or LivestockManager.sln above {Assembly.GetExecutingAssembly().Location})");

        string docsDir = Path.Combine(repoRoot, "docs");
        string auditDir = Path.Combine(repoRoot, "audit");

        var mdFiles = new List<string>();
        if (Directory.Exists(docsDir))
            mdFiles.AddRange(Directory.EnumerateFiles(docsDir, "*.md", SearchOption.AllDirectories));
        if (Directory.Exists(auditDir))
            mdFiles.AddRange(Directory.EnumerateFiles(auditDir, "*.md", SearchOption.AllDirectories));

        var violations = new List<string>();

        foreach (var filePath in mdFiles)
        {
            string fileName = Path.GetFileName(filePath);
            if (ExcludedFiles.Contains(fileName))
                continue;

            if (IsThirdPartyLicenseFile(filePath))
                continue;

            string rawContent = File.ReadAllText(filePath);
            string content = StripHistoricalQuoteRegions(rawContent);
            var fileViolations = FindForbiddenAssociations(content, filePath);
            violations.AddRange(fileViolations);
        }

        if (violations.Count > 0)
        {
            File.WriteAllLines(Path.Combine(FindRepoRoot(), "artifacts", "logs", "doc-consistency-violations.txt"), violations);
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void G_T1_CorrectCurrentMapping_PassesScanner()
    {
        string correctDoc = @"
# Current Correct Ovine Livestock Mappings

Authoritative mappings (all sheep terminology only):
- Ah = Purchased castrated ram (wether)
- Su = Uncastrated ram (intact male)
- Sa = Purchased ewe
- Ad = Bred castrated ram
- Sd = Bred ewe

All five codes map exclusively to ovine male and female descriptors.
";
        var violations = FindForbiddenAssociations(correctDoc, "G_T1_inline.md");
        Assert.Empty(violations);
    }

    [Fact]
    public void G_T2_FalseCurrentMappingWithoutMarkers_ReturnsNonzeroViolations()
    {
        string badDoc = @"
# This doc has incorrect unmarked associations

Old stale incorrect claims (NOT in historical markers — should be flagged):
- Ah = Aves poultry chicken
- Su = Swine pigs piglet
- Sa = Adult Bovine cow
- Ad = Young Bovine calf
- Sd = Purchased chick
";
        var violations = FindForbiddenAssociations(badDoc, "G_T2_bad_inline.md");
        Assert.True(violations.Count > 0,
            $"Expected nonzero violations for purposefully bad unmarked doc. Got {violations.Count}.");
    }

    [Fact]
    public void G_T3_MarkedHistoricalQuoteWrapped_NoViolations()
    {
        string docWithMarkers = @"
# Correct doc with wrapped historical baseline quotes

Current mappings:
- Ah = Purchased castrated ram
- Su = Uncastrated ram
- Sa = Purchased ewe
- Ad = Bred castrated ram
- Sd = Bred ewe

Historical quoted baseline (wrapped, must be ignored by scanner):
<!-- HISTORICAL_BASELINE_QUOTE_START -->
OLD incorrect claims recorded at original baseline audit 2026-08-07:
- Ah = Aves poultry chick hatchling
- Su = Swine pigs piglet
- Sa = Adult Bovine cow
- Ad = Young Bovine calf calves
- Sd = Purchased chick
<!-- HISTORICAL_BASELINE_QUOTE_END -->

Outside markers, only ovine terminology is used.
";
        string stripped = StripHistoricalQuoteRegions(docWithMarkers);
        var violations = FindForbiddenAssociations(stripped, "G_T3_marked_inline.md");
        Assert.Empty(violations);
    }

    [Fact]
    public void G_T4_FakeAuditFileWithoutMarkers_FalseStatementNonzero()
    {
        string fakeAuditBad = @"
# audit/fake-ROUND2-check.md — purposefully problematic synthetic

This simulates a NEW audit file (2026-08-09) that unapologetically writes:
Ah = Adult Bovine cow cattle Angus steer heifer bull ox.
Therefore scanner MUST return nonzero violations (no markers around it).
";
        var violations = FindForbiddenAssociations(fakeAuditBad, "audit/fake-ROUND2-check.md");
        Assert.True(violations.Count > 0,
            $"G_T4: unmarked false statement in new audit file should trigger >0 violations. Got {violations.Count}.");
    }

    [Fact]
    public void G_T5_SourceUiFiles_NoForbiddenStaleWordsAdjacentToFiveCodes()
    {
        string repoRoot = FindRepoRoot();
        var scanRoots = new[]
        {
            Path.Combine(repoRoot, "src"),
            Path.Combine(repoRoot, "views"),
            Path.Combine(repoRoot, "Pages"),
        };

        var sourceFiles = new List<string>();
        foreach (var root in scanRoots.Where(Directory.Exists))
        {
            sourceFiles.AddRange(Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories));
            sourceFiles.AddRange(Directory.EnumerateFiles(root, "*.cshtml", SearchOption.AllDirectories));
            sourceFiles.AddRange(Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories));
            sourceFiles.AddRange(Directory.EnumerateFiles(root, "*.js", SearchOption.AllDirectories));
        }

        var violations = new List<string>();
        foreach (var sf in sourceFiles)
        {
            string content;
            try { content = File.ReadAllText(sf); }
            catch { continue; }

            bool hasAnyCode =
                CodeAhWordBoundary.IsMatch(content) ||
                CodeSuWordBoundary.IsMatch(content) ||
                CodeSaWordBoundary.IsMatch(content) ||
                CodeAdWordBoundary.IsMatch(content) ||
                CodeSdWordBoundary.IsMatch(content);

            if (!hasAnyCode) continue;

            var fileViolations = FindForbiddenAssociations(content, sf);
            violations.AddRange(fileViolations);
        }

        Assert.Empty(violations);
    }

    private static bool IsThirdPartyLicenseFile(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        string dirName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? string.Empty);

        if (string.Equals(fileName, "LICENSE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "LICENSE.txt", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "LICENSE.md", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(dirName, "bootstrap", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, "jquery", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, "jquery-validation", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, "jquery-validation-unobtrusive", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    internal static List<string> FindForbiddenAssociations(string content, string filePath)
    {
        var violations = new List<string>();
        var words = TokenizeWords(content);
        int windowRadius = 10;

        for (int i = 0; i < words.Count; i++)
        {
            (string Word, int CharIndex) token = words[i];
            string w = token.Word;

            bool isLivestockCode = LivestockCodes.Contains(w, StringComparer.Ordinal);
            if (!isLivestockCode) continue;

            int start = Math.Max(0, i - windowRadius);
            int end = Math.Min(words.Count - 1, i + windowRadius);

            for (int j = start; j <= end; j++)
            {
                if (j == i) continue;

                string contextWord = words[j].Word;

                foreach (var forbidden in ForbiddenStaleWords)
                {
                    if (MatchesForbidden(contextWord, forbidden))
                    {
                        violations.Add(
                            $"File: {filePath} | " +
                            $"Code '{w}' at word index {i} | " +
                            $"Forbidden '{forbidden}' within ±{windowRadius} words (distance: {Math.Abs(j - i)}) | " +
                            $"Context: ...{SnippetAround(words, Math.Min(i, j), Math.Max(i, j))}..."
                        );
                    }
                }
            }
        }

        violations.AddRange(FindMultiWordForbiddenNearCodes(content, filePath));

        return violations;
    }

    private static List<string> FindMultiWordForbiddenNearCodes(string content, string filePath)
    {
        var violations = new List<string>();
        var multiWordPatterns = new Dictionary<Regex, string>
        {
            [new Regex(@"Adult\s+Bovine", RegexOptions.Compiled | RegexOptions.IgnoreCase)] = "Adult Bovine",
            [new Regex(@"Young\s+Bovine", RegexOptions.Compiled | RegexOptions.IgnoreCase)] = "Young Bovine",
            [new Regex(@"Purchased\s+chick", RegexOptions.Compiled | RegexOptions.IgnoreCase)] = "Purchased chick",
        };

        foreach (var (pattern, label) in multiWordPatterns)
        {
            foreach (Match m in pattern.Matches(content))
            {
                int matchCharIndex = m.Index;
                int matchCharEnd = m.Index + m.Length;

                bool nearCode = false;
                foreach (var code in LivestockCodes)
                {
                    var codeRegex = new Regex(@$"\b{Regex.Escape(code)}\b", RegexOptions.IgnoreCase);
                    foreach (Match cm in codeRegex.Matches(content))
                    {
                        int codeStart = cm.Index;
                        int distance = Math.Min(
                            Math.Abs(codeStart - matchCharIndex),
                            Math.Abs(codeStart - matchCharEnd));

                        int betweenChars = Math.Max(0,
                            Math.Min(matchCharEnd, codeStart + code.Length) -
                            Math.Max(matchCharIndex, codeStart));

                        if (betweenChars > 0 || CountWordsBetween(content, matchCharEnd, codeStart) <= 10 ||
                            CountWordsBetween(content, codeStart + code.Length, matchCharIndex) <= 10)
                        {
                            nearCode = true;
                            break;
                        }
                    }
                    if (nearCode) break;
                }

                if (nearCode)
                {
                    violations.Add(
                        $"File: {filePath} | " +
                        $"Multi-word forbidden phrase '{label}' (char {matchCharIndex}) within ±10 words of a livestock code"
                    );
                }
            }
        }

        return violations;
    }

    private static int CountWordsBetween(string content, int start, int end)
    {
        if (start >= end) return 0;
        int len = Math.Min(content.Length, end) - Math.Max(0, start);
        if (len <= 0) return 0;
        string sub = content.Substring(Math.Max(0, start), len);
        int count = 0;
        bool inWord = false;
        foreach (char c in sub)
        {
            if (char.IsLetterOrDigit(c))
            {
                if (!inWord) { count++; inWord = true; }
            }
            else inWord = false;
        }
        return count;
    }

    private static bool MatchesForbidden(string contextWord, string forbidden)
    {
        if (forbidden.Contains(' ')) return false;
        return string.Equals(contextWord, forbidden, StringComparison.OrdinalIgnoreCase);
    }

    private static List<(string Word, int CharIndex)> TokenizeWords(string content)
    {
        var result = new List<(string Word, int CharIndex)>();
        int i = 0;
        while (i < content.Length)
        {
            while (i < content.Length && !char.IsLetterOrDigit(content[i])) i++;
            if (i >= content.Length) break;
            int start = i;
            while (i < content.Length && char.IsLetterOrDigit(content[i])) i++;
            string word = content.Substring(start, i - start);
            result.Add((word, start));
        }
        return result;
    }

    private static string SnippetAround(List<(string Word, int CharIndex)> words, int startIdx, int endIdx)
    {
        int s = Math.Max(0, startIdx - 2);
        int e = Math.Min(words.Count - 1, endIdx + 2);
        var parts = new string[e - s + 1];
        for (int k = s; k <= e; k++) parts[k - s] = words[k].Word;
        return string.Join(" ", parts);
    }

    private static string FindRepoRoot()
    {
        string? current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
                return current;
            if (File.Exists(Path.Combine(current, "LivestockManager.sln")))
                return current;
            current = Path.GetDirectoryName(current);
        }
        return string.Empty;
    }
}
