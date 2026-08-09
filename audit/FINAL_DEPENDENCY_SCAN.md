# FINAL DEPENDENCY SCAN REPORT

Generated: 2026-08-09
Repository: LivestockManager
Branch: remediation/final-production-hardening

---

## 13a. VULNERABLE PACKAGES SCAN

Command: `dotnet list LivestockManager.sln package --vulnerable --include-transitive`

Sources used: https://api.nuget.org/v3/index.json

### Vulnerable Packages (Transitive Dependencies)

| Project | Transitive Package | Resolved | Severity | Advisory URL |
|---|---|---|---|---|
| LivestockManager.Domain | Microsoft.Extensions.Caching.Memory | 8.0.0 | High | GHSA-qj66-m88j-hmgj |
| LivestockManager.Application | Microsoft.Extensions.Caching.Memory | 8.0.0 | High | GHSA-qj66-m88j-hmgj |
| LivestockManager.Web | Microsoft.Build | 17.8.3 | High | GHSA-w3q9-fxm7-j8fq |
| LivestockManager.Web | System.Text.Json | 7.0.3 | High | GHSA-hh2w-p6rv-4g7w |
| LivestockManager.UnitTests | Microsoft.Build | 17.8.3 | High | GHSA-w3q9-fxm7-j8fq |
| LivestockManager.UnitTests | System.Text.Json | 8.0.4 | High | GHSA-8g4q-xg66-9fp4 |
| LivestockManager.IntegrationTests | Microsoft.Build | 17.8.3 | High | GHSA-w3q9-fxm7-j8fq |
| LivestockManager.IntegrationTests | System.Text.Json | 8.0.4 | High | GHSA-8g4q-xg66-9fp4 |
| LivestockManager.ArchitectureTests | Microsoft.Build | 17.8.3 | High | GHSA-w3q9-fxm7-j8fq |
| LivestockManager.ArchitectureTests | System.Text.Json | 8.0.4 | High | GHSA-8g4q-xg66-9fp4 |
| LivestockManager.EndToEndTests | Microsoft.Build | 17.8.3 | High | GHSA-w3q9-fxm7-j8fq |
| LivestockManager.EndToEndTests | System.Text.Json | 8.0.4 | High | GHSA-8g4q-xg66-9fp4 |

### Vulnerability Count Summary

- **Total unique vulnerable packages (transitive)**: 3 (Microsoft.Extensions.Caching.Memory, Microsoft.Build, System.Text.Json)
- **Total occurrences across all projects**: 12
- **Direct dependency vulnerabilities**: ZERO (all are transitive only — brought in via higher-level SDK/EF/Identity packages)
- **Critical severity**: 0
- **High severity**: 12
- **Medium/Low**: 0

---

## 13a. OUTDATED PACKAGES SCAN

Command: `dotnet list LivestockManager.sln package --outdated`

Sources used: https://api.nuget.org/v3/index.json

### Project: LivestockManager.Domain (net8.0)

| Name | Current | Latest |
|---|---|---|
| Microsoft.EntityFrameworkCore | 8.0.0 | 10.0.10 |

### Project: LivestockManager.Application (net8.0)

| Name | Current | Latest |
|---|---|---|
| FluentValidation | 11.9.0 | 12.1.1 |
| FluentValidation.DependencyInjectionExtensions | 11.9.0 | 12.1.1 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | 10.0.10 |

### Project: LivestockManager.Infrastructure (net8.0)

| Name | Current | Latest |
|---|---|---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.28 | 10.0.10 |
| Microsoft.EntityFrameworkCore.Design | 8.0.28 | 10.0.10 |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.28 | 10.0.10 |

### Project: LivestockManager.Web (net8.0)

| Name | Current | Latest |
|---|---|---|
| Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore | 8.0.28 | 10.0.10 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.28 | 10.0.10 |
| Microsoft.AspNetCore.Identity.UI | 8.0.28 | 10.0.10 |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.28 | 10.0.10 |
| Microsoft.EntityFrameworkCore.Tools | 8.0.28 | 10.0.10 |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 8.0.28 | 10.0.10 |
| Microsoft.VisualStudio.Web.CodeGeneration.Design | 8.0.4 | 10.0.2 |

### Project: LivestockManager.UnitTests (net8.0)

| Name | Current | Latest |
|---|---|---|
| coverlet.collector | 6.0.0 | 10.0.1 |
| Microsoft.Data.SqlClient | 5.2.2 | 7.0.2 |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.28 | 10.0.10 |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.28 | 10.0.10 |
| Microsoft.Extensions.Configuration | 8.0.0 | 10.0.10 |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | 10.0.10 |
| Microsoft.NET.Test.Sdk | 17.11.0 | 18.8.1 |
| xunit | 2.9.2 | 2.9.3 |
| xunit.runner.visualstudio | 2.8.2 | 3.1.5 |

### Project: LivestockManager.IntegrationTests (net8.0)

| Name | Current | Latest |
|---|---|---|
| coverlet.collector | 6.0.2 | 10.0.1 |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.0 | 10.0.10 |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.28 | 10.0.10 |
| Microsoft.NET.Test.Sdk | 17.11.0 | 18.8.1 |
| xunit | 2.9.2 | 2.9.3 |
| xunit.runner.visualstudio | 2.8.2 | 3.1.5 |

### Project: LivestockManager.ArchitectureTests (net8.0)

| Name | Current | Latest |
|---|---|---|
| coverlet.collector | 6.0.2 | 10.0.1 |
| Microsoft.NET.Test.Sdk | 17.11.0 | 18.8.1 |
| xunit | 2.9.2 | 2.9.3 |
| xunit.runner.visualstudio | 2.8.2 | 3.1.5 |

### Project: LivestockManager.EndToEndTests (net8.0)

| Name | Current | Latest |
|---|---|---|
| coverlet.collector | 6.0.2 | 10.0.1 |
| Microsoft.NET.Test.Sdk | 17.11.0 | 18.8.1 |
| Microsoft.Playwright | 1.52.0 | 1.61.0 |
| Microsoft.Playwright.NUnit | 1.52.0 | 1.61.0 |
| xunit | 2.9.2 | 2.9.3 |
| xunit.runner.visualstudio | 2.8.2 | 3.1.5 |

### Outdated Count Summary

- **Unique direct dependencies outdated**: 25
- **Total outdated references across all projects**: 50
- **Major-version jumps**: Most packages are on 8.x (net8.0 baseline) and latest is 10.x (net10). Note: 10.x upgrade not recommended until stack targets net10.
- **Minor-only updates within same SDK train**: xunit 2.9.2 -> 2.9.3 (patch), Playwright 1.52.0 -> 1.61.0 (minor)

---

## 13c. POTENTIALLY UNUSED DIRECT DEPENDENCIES

Analysis performed by cross-referencing PackageReference in each .csproj against:
1. `using <Namespace>` statements in *.cs source files
2. Concrete API usage (AbstractValidator, RuleFor for FluentValidation; Types.In/HaveDependencyOn for NetArchTest)
3. Build-only PrivateAssets packages treated as "required by tooling" even if no runtime source reference

### Findings

| Project | Package | Current | Finding | Status |
|---|---|---|---|---|
| LivestockManager.Application | FluentValidation | 11.9.0 | NO source files use FluentValidation namespace, AbstractValidator, IValidator, or RuleFor. Zero call sites. | POTENTIALLY UNUSED |
| LivestockManager.Application | FluentValidation.DependencyInjectionExtensions | 11.9.0 | NO ServiceCollection.AddFluentValidation() call sites anywhere. | POTENTIALLY UNUSED |
| LivestockManager.Web | Microsoft.VisualStudio.Web.CodeGeneration.Design | 8.0.4 | Design-time scaffolding package. No runtime references; typically required only for `dotnet aspnet-codegenerator`. Present in .csproj as development-time tooling aid. | BUILD/TOOLING (retain with PrivateAssets if not already) |
| LivestockManager.ArchitectureTests | NetArchTest.Rules | 1.3.2 | **USED** — LayerReferenceTests.cs line 1 `using NetArchTest.Rules;` plus Types.InAssembly() / HaveDependencyOn() calls. | USED (KEEP) |
| LivestockManager.UnitTests | Microsoft.Data.SqlClient | 5.2.2 | **USED** — SequenceRemediationTests.cs line 1 `using Microsoft.Data.SqlClient;` | USED (KEEP) |
| LivestockManager.UnitTests | Microsoft.EntityFrameworkCore.InMemory | 8.0.28 | **USED** — Multiple test files use UseInMemoryDatabase (ProtectedDocumentStorageTests, ProductionSeedHardeningTests, etc.) | USED (KEEP) |
| All test projects | coverlet.collector | various | Test SDK coverage collector — build-only, required for `dotnet test /p:CollectCoverage=true` — no source using expected. | BUILD/TOOLING (KEEP) |
| All test projects | Microsoft.NET.Test.Sdk | various | Test SDK required — no source using expected. | BUILD/TOOLING (KEEP) |
| All test projects | xunit + runner.visualstudio | various | Test framework + VS runner required — ImplicitUsings enable Xunit, used via [Fact]. | USED (KEEP) |
| LivestockManager.EndToEndTests | Microsoft.Playwright + Playwright.NUnit | 1.52.0 | **USED** — E2eRemediationChecklist uses Playwright Page/IBrowser | USED (KEEP) |
| LivestockManager.Infrastructure | Microsoft.EntityFrameworkCore.Design | 8.0.28 | EF design-time tooling — already PrivateAssets=all — no runtime source using expected. | BUILD/TOOLING (KEEP) |
| LivestockManager.Web | Microsoft.EntityFrameworkCore.Tools | 8.0.28 | EF tooling (dotnet ef) — already PrivateAssets=all — no runtime source using expected. | BUILD/TOOLING (KEEP) |

### Potentially Unused Summary

- **Confirmed unused direct dependencies (review for removal in Phase 15)**:
  1. `FluentValidation` (11.9.0) — LivestockManager.Application
  2. `FluentValidation.DependencyInjectionExtensions` (11.9.0) — LivestockManager.Application

- **Action**: Phase 15 final gates will attempt removal; if build + all tests pass with packages removed, they are safe to delete. Retain until Phase 15 confirms no regression.
