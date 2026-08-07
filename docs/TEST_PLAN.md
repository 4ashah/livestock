# TEST PLAN

## Quality Strategy & Test Suite Overview

---

## 1. Testing Pillars

```
┌──────────────────────────────────────────────────────────────────┐
│   PILLARS                                                         │
│                                                                   │
│   1. Unit Tests (xUnit)  — fast, in-process, no external deps    │
│   2. Integration Tests  — DB + Identity + File + Email in memory │
│   3. Architecture Tests — layer references, policy compliance    │
│   4. E2E Tests (Playwright, optional) — UI flows                 │
│   5. Manual QA / UAT — audit walkthroughs by finance stakeholder │
│   6. Penetration / Security Scan (OWASP ZAP)                     │
│   7. Performance Baseline (k6) — reports + dashboard load        │
└──────────────────────────────────────────────────────────────────┘
```

### Target Metrics

| Metric | Threshold | Gate |
|---|---|---|
| Unit + Integration code coverage (Application + Domain combined) | ≥ 80% line, ≥ 70% branch | Build fails below |
| Unit test execution time | ≤ 3 min for entire suite | CI parallelized |
| Integration test execution time | ≤ 10 min | CI; nightly extended suite |
| Critical/High open bugs | 0 before release | Go/No-Go |
| Open Medium bugs | ≤ 3 before release | Business sign-off for each |
| Open security findings (ZAP) Critical/High | 0 | Release gate |
| Lighthouse Performance Score (dashboard) | ≥ 70 | Informational |

---

## 2. Unit Tests (xUnit)

### 2.1 Framework

- **Runner**: xUnit 2.7+ with `dotnet test`.
- **Isolation**: class-level `IClassFixture<T>`; no shared static mutable state.
- **Mocks**: `Moq 4.20+` (or `NSubstitute`); strict mock behavior by default.
- **Assertions**: `FluentAssertions` 6.0+ (natural language assertions).
- **Data**: `AutoFixture` for test data generation; `Bogus` for realistic fake customer/supplier names.
- **Code Coverage**: `Coverlet.Collector` (built-in), output Cobertura XML to `./artifacts/coverage`.

### 2.2 Project Structure

```
tests/LivestockManagement.Application.UnitTests
├── Features/
│   ├── Companies/
│   │   ├── CreateCompanyCommandTests.cs
│   │   ├── UpdateCompanyCommandTests.cs
│   │   ├── DeleteCompanyCommandTests.cs
│   │   ├── ListCompaniesQueryTests.cs
│   │   └── Validators/
│   │       └── CreateCompanyCommandValidatorTests.cs
│   ├── Farms/     (same pattern)
│   ├── Livestock/ (same pattern)
│   ├── Customers/ (same pattern)
│   ├── Suppliers/ (same pattern)
│   ├── Purchases/
│   ├── Expenses/
│   ├── SalesOrders/
│   ├── Invoices/
│   ├── Payments/
│   ├── Receipts/
│   └── Reports/
├── Behaviors/
│   ├── ValidationBehaviorTests.cs
│   ├── UnitOfWorkBehaviorTests.cs
│   └── AuthorizationBehaviorTests.cs
├── DomainServices/
│   ├── LivestockValuationServiceTests.cs
│   ├── PaymentAllocationServiceTests.cs
│   └── SequenceNumberGeneratorTests.cs
├── Mapping/
│   └── MapsterConfigurationTests.cs (assert no unmapped members)
└── GlobalUsings.cs
```

### 2.3 Coverage Target Areas

| Area | What is tested |
|---|---|
| CQRS Command Handlers | happy path + validation failure paths + concurrency exceptions + domain invariant violations |
| CQRS Query Handlers | returns correct shape of DTO; respects pagination; applies company filter; empty result when no data |
| FluentValidators | each rule: required, max-length, range, enum range, format (email/phone) |
| Domain Entities | constructor invariants, status transitions, throw on invalid, setters that check business rules |
| Domain Services | livestock valuation weighted-average math, payment allocation edge cases: overpayment, underpayment, multi-invoice partial |
| Sequence Generator | concurrent calls produce unique numbers; year roll; separate counters per company per doc type |
| Value Objects | Money +/−/*, Weight kg↔lb conversion accuracy to 4 decimals |

### 2.4 Example: Payment Allocation Tests

```csharp
[Theory]
[InlineData( 100.00, new[] { 60.00, 40.00 }, new[] { 60.00, 40.00 } )] // exact two
[InlineData( 100.00, new[] { 200.00       }, new[] { 100.00       } )] // partial
[InlineData( 500.00, new[] { 200.00, 100 }, new[] { 200.00, 100, 200 cr } )] // overpayment -> credit
public void PaymentAllocator_AllocateByOldestDue_FollowsExpectedPlan(
    decimal paymentAmount, decimal[] invoiceBalances, decimal[] expectedAllocations)
{
    // Arrange
    var allocator = new PaymentOldestFirstAllocator(_clock);
    var invoices = invoiceBalances.Select((b, i) =>
        BuildInvoice(dueDate: _jan1.AddDays(i), balance: b, id: Guid.NewGuid())).ToList();

    // Act
    var plan = allocator.Allocate(paymentAmount, invoices);

    // Assert
    plan.TotalAllocated.Should().Be(paymentAmount);
    plan.Allocations.Zip(expectedAllocations).Should().AllSatisfy(pair =>
        pair.First.Amount.Should().BeApproximately(pair.Second, 0.01m));
}
```

---

## 3. Integration Tests

### 3.1 Test Host

- WebApplicationFactory (`Microsoft.AspNetCore.Mvc.Testing`) boots a real Kestrel with:
  - Database: `Testcontainers.MsSql` (ephemeral SQL Server container per test class, OR in-memory Sqlite for simpler non-relational tests — default: SQL container so we get real constraints, triggers, FKs, rowversion).
  - Identity: real stores, same DbContext.
  - Email: `IEmailSender` replaced with `InMemoryEmailSender` (captures messages for assertions).
  - File storage: `InMemoryFileStorage` (byte[] dictionary keyed by storageKey).
  - Hangfire: disabled or in-memory storage, no background processing.
  - Time: `IDateTime` frozen to `2026-01-15 10:00 UTC`.

### 3.2 Scenarios Covered

| ID | Scenario | Entry | Asserts |
|---|---|---|---|
| INT-001 | Company isolation: UserA (Company1) cannot see UserB (Company2) invoices | GET /Invoices with UserA JWT/cookie | Count = 0 for Company2 rows; SQL Profiler confirms WHERE CompanyId = A |
| INT-002 | Purchase post → creates Livestock rows + WeightEntry | POST /Purchases/id/post | Livestock.Count += Qty; Stock-on-hand query reflects change; DocumentSequence incremented |
| INT-003 | Full end-to-end happy path: Create Customer → Create SalesOrder → Create Invoice → Send → Record Payment | 5 HTTP calls via HttpClient | Invoice.Status → Paid; Livestock.Status = Sold; Customer.Balance 0; PaymentAllocations rows; AuditLogs 8+ entries; Email queued (in sender outbox) |
| INT-004 | Login wrong password lockout after 5 attempts | 6 POSTs to /login | 6th = 403; LockoutEnd set; AuditLog lockout entry |
| INT-005 | TOTP enrollment + login flow | API calls | Authenticator key generated; token validated; recovery codes hashed not stored plain |
| INT-006 | Invoice void → livestock status revert → no double-counting | POST /Invoices/id/void → Livestock detail | Livestock.Status = Alive; BalanceDue 0; GrandTotal excluded from A/R report totals |
| INT-007 | Overdue aging bucket calculation on 30/31/60/90 boundaries | Freeze clock on day offsets, run A/R report | Bucket amounts within tolerance 0.01 |
| INT-008 | Upload file — disallowed extension (e.g. `.exe`) | Multipart POST | 400; StoredFiles row not created; 10 MB pdf = accepted; 11 MB = rejected with 413 |
| INT-009 | Upload file — allow-listed extension + wrong magic bytes (rename `.exe` to `.pdf`) | Multipart POST of PE binary with pdf ext | 400 — "file signature invalid" |
| INT-010 | Export CSV + Excel of 500 invoices | GET /Reports/Sales/Export?format=csv+xlsx | CSV can be opened by a parser; Excel opens without corruption; row count matches DB |
| INT-011 | Dev-user seed block on ASPNETCORE_ENVIRONMENT=Production | Build WebApplicationFactory with Production env + ENV_ENABLE_DEV_SEED=true | Assert 0 users with email @example.com exist after db init |
| INT-012 | Identity: expired session → force redirect to login | Issue cookie with Past expiry | Navigation to dashboard returns 302 to /Identity/Account/Login |
| INT-013 | Cross-company tag ID uniqueness: Company1 tag A100 + Company2 tag A100 allowed, same company duplicate rejected | 3 inserts | Third insert throws ValidationException; UNIQUE filtered index verified |
| INT-014 | Concurrency: 2 parallel POSTs edit same invoice rowversion; one wins 200, one 409 conflict | Parallel Tasks via Semaphore | Error response to loser with helpful message |
| INT-015 | Report totals cross-check: P&L Net Profit calculated equals Sales Total − Purchases Total − Expenses Total for same period | SQL query vs report endpoint | Δ ≤ 0.02 (rounding tolerance) |

### 3.3 Data Seeding Helper

Integration test class fixture provides:
- `CreateUserAsync(Role role, Company company) → (User + ClaimsPrincipal + AuthCookie)`.
- `SeedInvoicesForCompanyAsync(company, count=50, randomStatuses)`.
- `SeedLivestockAsync(company, farm, count=200)`.
Seeded data is DISPOSABLE (each test class has its own SQL container, or schema reset via `Respawn` package before each test).

---

## 4. Architecture Tests

### 4.1 Framework

**NetArchTest.Rules** (`ArchUnit` alternative for .NET) runs as xUnit tests. Project: `LivestockManagement.Web.ArchitectureTests`.

### 4.2 Rules

| ID | Rule | Severity |
|---|---|---|
| ARCH-001 | Domain assembly has no dependencies on Application, Infrastructure, or Web. | FAIL build |
| ARCH-002 | Application depends only on Domain (not Infrastructure/Web). | FAIL build |
| ARCH-003 | Infrastructure depends only on Application + Domain; must never reference Web. | FAIL build |
| ARCH-004 | Web project can reference Domain/Application/Infrastructure; direct references to `Microsoft.EntityFrameworkCore` (except DI registration file) are forbidden → use abstractions. | FAIL build |
| ARCH-005 | Every class named `*Command`/`*Query` implements `MediatR.IRequest<TResponse>` or `IRequest`. | FAIL build |
| ARCH-006 | Every class named `*CommandHandler`/`*QueryHandler` implements `IRequestHandler<T, R>`. | FAIL build |
| ARCH-007 | Every class named `*Validator` inherits `AbstractValidator<T>`. | WARN |
| ARCH-008 | Entities in Domain have NO references to `Microsoft.EntityFrameworkCore` (no `[Key]` attributes allowed — fluent API only). | FAIL build |
| ARCH-009 | `LivestockDbContext` lives ONLY in Infrastructure assembly. | FAIL build |
| ARCH-010 | All Controllers and Razor Page models require `[Authorize]` attribute (except /Identity, /health, /error pages explicitly listed). | WARN |
| ARCH-011 | No `[Authorize(Roles = "roleName")]` attribute usage; only `[Authorize(Policy = nameof(Policy.Xxx))]` allowed. | FAIL build |
| ARCH-012 | No `DateTime.Now` usage anywhere (use `DateTime.UtcNow` or abstracted `IDateTime.Now`). | WARN |
| ARCH-013 | Connection strings, API keys: `IConfiguration` string access must NOT use hard-coded literal `"Password="` (prevent accidental secrets-in-code smell test). | WARN |

Example test:

```csharp
[Fact]
public void Domain_Should_HaveNoDependenciesOnOtherProjectAssemblies()
{
    var result = Types
        .InAssembly(DomainAssembly)
        .Should()
        .NotHaveDependencyOn("LivestockManagement.Application")
        .And().NotHaveDependencyOn("LivestockManagement.Infrastructure")
        .And().NotHaveDependencyOn("LivestockManagement.Web")
        .And().NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
        .GetResult();
    Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames));
}
```

---

## 5. E2E Tests (Playwright — Optional)

### 5.1 Scope

- **Not required for MVP** — planned for post-MVP release if regression churn on UI > acceptable.
- Browser: Chromium + Firefox (WebKit optional). Run via `dotnet test` or `npx playwright test`.

### 5.2 Scenarios (Priority 1)

| ID | Scenario |
|---|---|
| E2E-001 | Login with valid credentials → dashboard KPIs render → logout. |
| E2E-002 | Register new livestock: complete form, upload photo, submit → list shows row, count on dashboard incremented. |
| E2E-003 | Create invoice from sales order, view PDF preview in new tab, send via email. |
| E2E-004 | Role-based menu: login as Viewer → Invoices > "New Invoice" button hidden + direct URL returns 403. |
| E2E-005 | Run P&L report with custom date range, export to Excel, open in workbook without warnings. |
| E2E-006 | Company admin invites user → invitee clicks link, sets password → assigned role works. |

### 5.3 Stability

- Retry policy: every test retries 1× on failure (flaky detection).
- Video recording on failure; artifacts uploaded to CI run.

---

## 6. Quality Gates (CI Pipeline)

### 6.1 Pull-Request Pipeline (runs on every PR)

```
┌────────────────────────────────────────────────────────────┐
│ Step 1: dotnet restore                                     │
│ Step 2: dotnet build (warn as errors on)                   │
│ Step 3: dotnet format style --verify-no-changes            │
│ Step 4: Unit tests (Application.UnitTests + Domain.UnitTests)
│          ▶ with coverage threshold 80% FAIL if not met     │
│ Step 5: Architecture tests                                 │
│          ▶ FAIL if any architectural rule broken           │
│ Step 6: Security: dotnet list package --vulnerable         │
│          ▶ FAIL if Critical or High severity in transitive │
│ Step 7: SonarQube / CodeQL static analysis (PR comments)   │
│          ▶ FAIL if Quality Gate = Red on new code          │
└────────────────────────────────────────────────────────────┘
```

### 6.2 Main / Release Branch Pipeline (runs on merge)

```
┌────────────────────────────────────────────────────────────┐
│ All PR steps +:                                             │
│ Step 8: Integration tests (SQL Server container)           │
│          ▶ 100% pass required for merge commit status      │
│ Step 9: OWASP ZAP baseline scan against staging (DAST)     │
│          ▶ FAIL if Critical/High risk alerts                │
│ Step 10: Performance smoke — k6 hits dashboard 50 rps      │
│          ▶ P95 < 2.5 s                                      │
│ Step 11: Publish artifacts: web.zip, migrations.sql        │
└────────────────────────────────────────────────────────────┘
```

### 6.3 Go/No-Go for Production Release

Required sign-offs before v1.0 deploy:

| Area | Sign-off |
|---|---|
| 100% Unit + Architecture + Integration tests green | Engineering Lead |
| 0 Critical / 0 High security findings (ZAP + SAST) | Security Champion |
| UAT test pass report (business users) | Business Owner |
| Performance baseline 50 rps stable + no errors | SRE |
| Backup / restore drill on staging completed | DBA |
| Rollback plan documented and validated against staging | DevOps |
| Documentation set (all docs/ files) reviewed against final product | Product Manager |

---

## 7. Regression Policy

- Any critical bug fixed in production **must** include a regression test in the Unit or Integration suite that proves the fix and prevents future recurrence.
- Bug report → PR = code change + regression test + proof screenshot; PR checklist item "regression test added" enforced by template.

---

## 8. Data Compliance Tests

| Test | What |
|---|---|
| GDPR-001 | User deletion request → `IsDeleted=1` on user, cascaded audit de-identification in AuditLogs (UserDisplayName → REDACTED, IpAddress + UserAgent nulled) within 30 days. |
| GDPR-002 | Customer data export: all columns listed in DATABASE.md for one CustomerId including associated Invoices/Payments exported to ZIP with CSV files. |
| TAX-001 | All invoice numbers in a year are contiguous (no gaps between min and max with no voided docs counted in gap list; gap list report generated). |
| TAX-002 | Totals on invoice PDF bytes compare equal to DB row (hash of canonical fields matches on random sample of 50 invoices). |
