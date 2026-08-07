# MASTER PLAN

## Livestock Management Invoicing System — .NET

---

## Phase 1: Project Setup & Solution Structure

**Objective:** Establish the foundational solution skeleton with clean architecture layering.

| Item | Detail |
|---|---|
| Duration | 5 days |
| Deliverables | `.sln` file, 4 core projects (Domain, Application, Infrastructure, Web), `Directory.Build.props`, `.editorconfig`, CI pipeline YAML |
| Key Tasks | Create solution with class libraries and Razor Pages/MVC web project; configure `Directory.Build.props` for shared versioning, nullable enable, ImplicitUsings; set up Git `.gitignore`; configure CI (GitHub Actions or Azure DevOps) for restore + build; define root `README.md` placeholder |
| Exit Criteria | `dotnet restore && dotnet build` succeeds; all projects reference correctly per architecture diagram |

---

## Phase 2: Domain Model & Database Schema

**Objective:** Define entities, value objects, enums, and the EF Core model mapping.

| Item | Detail |
|---|---|
| Duration | 8 days |
| Deliverables | Domain entities (Company, Farm, Livestock, Customer, Supplier, Purchase, Expense, Sale, Invoice, Payment, Receipt, Report*queries not persisted*), enums (LivestockType Ah/Su/Sa/Ad/Sd, DocumentStatus, PaymentMethod), EF Core `LivestockDbContext`, fluent API configurations, initial migration |
| Key Tasks | Design all entities with PK, FK, unique constraints, indexes; map `decimal(18,2)` for money, `decimal(18,4)` for weights; add `IsDeleted` (soft delete), `CreatedAt`/`ModifiedAt` UTC, `Version` concurrency token; add global query filters for `IsDeleted` and `CompanyId`; scaffold initial migration |
| Exit Criteria | `dotnet ef database update` succeeds on LocalDB; schema verified via SSMS; all unique/index constraints in place |

---

## Phase 3: Application Layer — CQRS & Business Logic

**Objective:** Implement MediatR CQRS handlers for all core modules.

| Item | Detail |
|---|---|
| Duration | 12 days |
| Deliverables | Commands (Create*/Update*/Delete*) and Queries (Get*/List*/Search*) for Companies/Farms, Livestock, Customers/Suppliers, Purchases/Expenses, Sales/Invoicing, Payments/Receipts; FluentValidation validators; company-scoped sequence counters; AutoMapper profiles |
| Key Tasks | Implement `IRequest<>` handlers in Application project; wire FluentValidation pipeline behavior; implement invoice number generation (company-scoped sequence with year reset); implement livestock weight tracking with unit conversion (kg↔lb); implement purchase/sale stock movement logic; add repository abstractions with EF Core implementations in Infrastructure |
| Exit Criteria | All handlers pass unit tests; validation rejects invalid data; sequence counters are unique per company per document type |

---

## Phase 4: Infrastructure Layer — Persistence & Identity

**Objective:** Wire up EF Core, ASP.NET Core Identity, file storage, PDF generation, email queue.

| Item | Detail |
|---|---|
| Duration | 10 days |
| Deliverables | `LivestockDbContext` implementation with repositories; Identity registration with extended `ApplicationUser` (CompanyId, FullName, IsEnabled); Role seeding (Viewer/DataEntry/FarmManager/Accounts/CompanyAdministrator/SystemAdministrator); `IFileStorage` with local disk and Azure Blob implementations; `IPdfGenerator` abstraction; `IEmailQueue` abstraction with Hangfire/background service; audit logging interceptor |
| Key Tasks | Configure Identity stores; add default admin user seed (only in dev, blocked in prod via `ASPNETCORE_ENVIRONMENT != Production` + explicit env switch); implement global `CompanyId` query filter enforced via `SaveChangesAsync` override; wire audit log for Create/Update/Delete; implement Hangfire for email queue and PDF generation |
| Exit Criteria | Identity login works; role-based authorization applied to sample endpoint; company isolation verified (User cannot see other Company records); audit log entries created for mutations |

---

## Phase 5: Web UI — Companies, Farms, Users

**Objective:** Build administration UI for company onboarding, farm management, user/role management.

| Item | Detail |
|---|---|
| Duration | 8 days |
| Deliverables | Razor Pages/Views for Company CRUD, Farm CRUD (linked to Company), User list + role assignment, login page, layout with sidebar navigation; flash-message TempData helper; jQuery DataTables or similar for list views |
| Key Tasks | Scaffold Identity UI and customize login/register; implement company picker for SystemAdministrator; enforce `[Authorize(Policy=...)]` on all pages; role-based menu rendering; implement anti-CSRF tokens globally |
| Exit Criteria | SystemAdministrator can create/assign companies; CompanyAdministrator can manage farms and invite users; role-based menu visibility verified for all 6 roles |

---

## Phase 6: Web UI — Livestock & Inventory

**Objective:** Build livestock registry with lifecycle tracking and weight history.

| Item | Detail |
|---|---|
| Duration | 10 days |
| Deliverables | Livestock list/grid with filters (farm, type Ah/Su/Sa/Ad/Sd, status, date range); create/edit livestock with tag ID, type, DOB, gender, initial weight; weight entry history page; livestock status transitions (Alive → Sold/Slaughtered/Dead); stock-on-hand dashboard widget |
| Key Tasks | Implement tag ID unique per company validator; weight unit selector (kg/lb) stored per user preference with conversion; implement file upload for livestock photos (.jpg/.png only, allow-list enforced); implement DataTables server-side filtering for large herds |
| Exit Criteria | Livestock CRUD works with validation; weight history displays correctly in user's preferred unit; livestock count on dashboard matches DB `WHERE IsDeleted = 0 AND Status = Alive` |

---

## Phase 7: Web UI — Purchases & Expenses

**Objective:** Implement purchase orders (livestock intake) and general expense tracking.

| Item | Detail |
|---|---|
| Duration | 8 days |
| Deliverables | Supplier CRUD; Purchase create with line items (livestock batch + unit price), auto-calc totals + 20% default configurable VAT; Purchase receipt with attachment upload (.pdf); Expense CRUD with categories (Feed, Vet, Transport, Utilities, Other); purchase-list with filters |
| Key Tasks | Implement purchase-number sequence (company-scoped); on Purchase approved, auto-create Livestock records or update stock; implement line-item add/remove client-side with total recalculation; enforce `CompanyId` FK on supplier, purchase, expense; PDF upload allow-list + size 10MB limit |
| Exit Criteria | Creating a Purchase with livestock items increases stock-on-hand; expense total + tax computes correctly; attachments stored under `{CompanyId}/{PurchaseId}/` path |

---

## Phase 8: Web UI — Sales & Invoicing

**Objective:** Implement customer management, sales orders, and invoice generation with PDF.

| Item | Detail |
|---|---|
| Duration | 12 days |
| Deliverables | Customer CRUD; Sales Order create with livestock line items (qty, unit price, discount); Invoice generation from Sales Order (or direct); invoice PDF download; invoice list with status (Draft/Sent/Paid/Overdue/Void); Send invoice via email (queued); credit-note stub; 30-day default due date |
| Key Tasks | Invoice number sequence (company-scoped, YY-NNNNN format); PDF generation via `IPdfGenerator` (QuestPDF or similar); invoice totals: subtotal, discount, taxable amount, VAT 20% configurable, grand total; email queue with SendGrid/SMTP abstraction; On Invoice Paid → mark livestock as Sold; compute aging buckets (0-30, 31-60, 61-90, 90+) |
| Exit Criteria | End-to-end: Customer → Sales Order → Invoice → PDF generated → email queued → livestock status = Sold; overdue flag updates based on DueDate vs today |

---

## Phase 9: Web UI — Payments, Receipts & Reports

**Objective:** Implement payment allocation, receipt printing, and reports module.

| Item | Detail |
|---|---|
| Duration | 10 days |
| Deliverables | Payments (Customer → Invoice, multi-invoice allocation, over-payment credit); Receipts (Supplier payment); Reports: Sales by Period, Purchases by Period, Livestock Valuation (weight × avg price), A/R Aging, A/P Aging, Profit & Loss Summary, Expense by Category; CSV/Excel export; dashboard KPI tiles |
| Key Tasks | Implement payment allocation algorithm (apply to oldest invoice first, or manual line selection); implement report queries as MediatR `IQuery<ReportDto>` with `AsNoTracking`; CSV export via CsvHelper; Excel via ClosedXML; dashboard: total sales MTD, total purchases MTD, receivables overdue, livestock count, net profit MTD |
| Exit Criteria | Payment correctly reduces invoice balance and marks Paid when sum reaches grand total; all 8 reports run without error; CSV export opens cleanly in Excel; dashboard KPIs match report totals |

---

## Phase 10: Security Hardening, Testing & Quality Gates

**Objective:** Execute security review, complete test suites, enforce quality gates.

| Item | Detail |
|---|---|
| Duration | 10 days |
| Deliverables | Completed unit tests (xUnit) for all Application handlers; integration tests for DB + Identity; architecture tests (NetArchTest) enforcing layer references; security penetration checklist; XSS/CSRF/SQLi review; file upload restrictions validated |
| Key Tasks | Achieve >80% code coverage on Application and Domain; architecture tests assert Web→Infra→App→Domain only, no reverse refs; run OWASP ZAP baseline scan against staging; validate dev-users-seed is disabled when `ENV_ENABLE_DEV_SEED != true` in any env including staging; verify company isolation via integration tests (2 companies, 1 user each, cross-read returns 0 rows) |
| Exit Criteria | All unit, integration, architecture tests green in CI; ZAP zero Critical/High; dev-seed blocked on Staging/Production confirmed |

---

## Phase 11: Deployment, Monitoring & Handover

**Objective:** Deploy to production IIS, configure monitoring, produce admin/user guides.

| Item | Detail |
|---|---|
| Duration | 7 days |
| Deliverables | Production IIS site running HTTPS; app-pool identity with least-privilege ACLs; Serilog file + SEQ sink; health-check endpoint `/health`; firewall rules locked to 443; backup plan (SQL full + log backups, blob/file storage backup); deployment runbook; user + admin training walkthrough |
| Key Tasks | Install .NET Hosting Bundle on Windows Server; create IIS App Pool (No Managed Code, AppPoolIdentity); set ACLs on `wwwroot`, `logs`, `App_Data`; apply migration via `dotnet ef database update` (or generate idempotent SQL script); configure HTTPS cert (Let's Encrypt/CA); set `ASPNETCORE_ENVIRONMENT=Production` + env vars for ConnectionString, Storage, Email API keys; validate backup/restore with test DB |
| Exit Criteria | Public URL loads over HTTPS with HSTS; `/health` returns Healthy; email send works; PDF download works; backup-restore drill succeeds; sign-off on documentation |

---

## Summary Timeline

| Phase | Name | Duration (days) |
|---|---|---|
| 1 | Project Setup & Solution Structure | 5 |
| 2 | Domain Model & Database Schema | 8 |
| 3 | Application Layer — CQRS & Business Logic | 12 |
| 4 | Infrastructure — Persistence & Identity | 10 |
| 5 | Web UI — Companies, Farms, Users | 8 |
| 6 | Web UI — Livestock & Inventory | 10 |
| 7 | Web UI — Purchases & Expenses | 8 |
| 8 | Web UI — Sales & Invoicing | 12 |
| 9 | Web UI — Payments, Receipts & Reports | 10 |
| 10 | Security Hardening, Testing & Quality Gates | 10 |
| 11 | Deployment, Monitoring & Handover | 7 |
| **Total** | | **100** |
