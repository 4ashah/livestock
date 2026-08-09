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

            string content = File.ReadAllText(filePath);
            var fileViolations = FindForbiddenAssociations(content, filePath);
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

    private static List<string> FindForbiddenAssociations(string content, string filePath)
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
