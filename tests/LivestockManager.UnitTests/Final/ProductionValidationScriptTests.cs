using System;
using System.IO;
using Xunit;

namespace LivestockManager.UnitTests.Final;

public class ProductionValidationScriptTests
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static readonly string ScriptsRoot = Path.Combine(RepoRoot, "scripts");

    private static string ReadAllTextSafe(string path)
    {
        Assert.True(File.Exists(path), "Script file should exist: " + path);
        return File.ReadAllText(path);
    }

    [Fact]
    public void S1_ValidateProductionConfig_Exists_HasCmdletBinding_DeclaresServerInstanceParam()
    {
        string scriptPath = Path.Combine(ScriptsRoot, "Validate-ProductionConfig.ps1");
        string content = ReadAllTextSafe(scriptPath);

        Assert.Contains("[CmdletBinding()]", content, StringComparison.Ordinal);
        Assert.Contains("param(", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ServerInstance", content, StringComparison.Ordinal);
    }

    [Fact]
    public void S2_CheckPrerequisites_Exists_CallsDotNetVersion_Keywords()
    {
        string scriptPath = Path.Combine(ScriptsRoot, "Check-Prerequisites.ps1");
        string content = ReadAllTextSafe(scriptPath);

        Assert.Contains("dotnet --version", content, StringComparison.Ordinal);
        Assert.Contains("sqlcmd.exe", content, StringComparison.Ordinal);
        Assert.Contains("disk", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S3_NewFirstProductionAdmin_Exists_ContainsReadHostAsSecureString()
    {
        string scriptPath = Path.Combine(ScriptsRoot, "New-FirstProductionAdmin.ps1");
        string content = ReadAllTextSafe(scriptPath);

        Assert.Contains("Read-Host", content, StringComparison.Ordinal);
        Assert.Contains("-AsSecureString", content, StringComparison.Ordinal);
        Assert.Contains("SecureStringToBSTR", content, StringComparison.Ordinal);
        Assert.Contains("ZeroFreeBSTR", content, StringComparison.Ordinal);
        Assert.Contains("--first-admin", content, StringComparison.Ordinal);
    }

    [Fact]
    public void S4_ExistingProductionScripts_Exist_WithExpectedFunctionNames()
    {
        string publishPath = Path.Combine(ScriptsRoot, "Publish-IIS.ps1");
        string backupPath = Path.Combine(ScriptsRoot, "Backup-Database.ps1");
        string restorePath = Path.Combine(ScriptsRoot, "Restore-Database.ps1");

        Assert.True(File.Exists(publishPath), "Publish-IIS.ps1 should exist");
        Assert.True(File.Exists(backupPath), "Backup-Database.ps1 should exist");
        Assert.True(File.Exists(restorePath), "Restore-Database.ps1 should exist");

        string backupContent = File.ReadAllText(backupPath);
        string restoreContent = File.ReadAllText(restorePath);

        Assert.Contains("BACKUP DATABASE", backupContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RESTORE DATABASE", restoreContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S5_RunE2ETests_Exists_DeclaresServerInstanceParam()
    {
        string scriptPath = Path.Combine(ScriptsRoot, "Run-E2ETests.ps1");
        string content = ReadAllTextSafe(scriptPath);

        Assert.Contains("[CmdletBinding()]", content, StringComparison.Ordinal);
        Assert.Contains("param(", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ServerInstance", content, StringComparison.Ordinal);
    }
}
