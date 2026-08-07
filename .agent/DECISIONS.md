# Architecture Decisions Record (ADR)

## ADR-001: .NET 8 LTS as Target Framework
- **Date**: 2026-08-06
- **Status**: Accepted
- **Context**: Project needs a stable, well-supported .NET platform for long-term maintenance.
- **Decision**: Target .NET 8 LTS for all projects in the solution.
- **Consequences**: Long-term support from Microsoft, access to latest .NET APIs, compatible with EF Core 8.

## ADR-002: Entity Framework Core 8 for Data Access
- **Date**: 2026-08-06
- **Status**: Accepted
- **Context**: ORM required for SQL Server data access with migration support.
- **Decision**: Use EF Core 8 with SQL Server provider.
- **Consequences**: Built-in migrations, LINQ query support, change tracking, repository pattern friendly.

## ADR-003: SQL Server as Database
- **Date**: 2026-08-06
- **Status**: Accepted
- **Context**: Relational database for livestock management data with strong consistency requirements.
- **Decision**: Use SQL Server (LocalDB for development, full SQL Server for production).
- **Consequences**: Good EF Core integration, strong query optimization, common enterprise deployment.

## ADR-004: ASP.NET Core MVC for Web Application
- **Date**: 2026-08-06
- **Status**: Accepted
- **Context**: Traditional server-rendered web UI for CRUD operations and reports.
- **Decision**: Use ASP.NET Core MVC pattern (controllers + views + Razor).
- **Consequences**: Familiar development model, good for form-heavy enterprise apps, supports validation via data annotations.

## ADR-005: Bootstrap 5 for Frontend Styling
- **Date**: 2026-08-06
- **Status**: Accepted
- **Context**: Responsive, modern UI components without heavy frontend framework.
- **Decision**: Use Bootstrap 5 CSS/JS (via CDN or libman in Web project).
- **Consequences**: Consistent responsive layout, form controls, tables, modals out-of-the-box. Reduces custom CSS.

## ADR-006: Clean Architecture Layers
- **Date**: 2026-08-06
- **Status**: Accepted
- **Context**: Maintainable separation of concerns, testability, dependency inversion.
- **Decision**: Organize solution into 4 layers:
  - **Domain**: Entities, value objects, enums, domain interfaces (no infra dependencies)
  - **Application**: Use cases (CQRS commands/queries), DTOs, repository interfaces, services (depends on Domain)
  - **Infrastructure**: EF Core, repository implementations, external services (depends on Application + Domain)
  - **Web**: MVC controllers, views, UI (depends on Application)
- **Consequences**: Clear separation, easy unit testing, infrastructure can be replaced without touching domain logic.

## ADR-007: Decimal for Money and Weights
- **Date**: 2026-08-06
- **Status**: Accepted
- **Context**: Financial values (prices, costs, totals) and livestock weights require precision and avoid floating-point errors.
- **Decision**: Use C# `decimal` type (128-bit) for all monetary fields and weight measurements. Configure EF Core to use `decimal(18,2)` for money and `decimal(10,3)` for weights via explicit model configuration.
- **Consequences**: No precision loss in calculations, consistent database storage.

======================================================
MVP RESCOPE DECISIONS (2026-08-07)
======================================================

## ADR-MVP-001: Keep 4-project layered structure
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: Collapsing layers to 2+1 projects would require namespace refactor, using statement rewrites, and reconfiguring DI across the codebase.
- **Decision**: Keep the existing 4-project structure (Domain / Application / Infrastructure / Web) because it already builds cleanly.
- **Consequences**: Avoids a risky refactor and lets MVP focus on feature cuts instead of layout churn.

## ADR-MVP-002: 4 roles only
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: The original plan distinguished CompanyAdministrator vs SystemAdministrator, but MVP deploys a single company.
- **Decision**: 4 roles only: Administrator, Manager, DataEntry, Viewer. Administrator is the top role (single-company deployment).
- **Consequences**: Role checks become trivial, UI can harden gatekeeping via simple `[Authorize(Roles = ...)]` attributes.

## ADR-MVP-003: Single-company deployment
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: Multi-company switcher and per-company scoping logic add significant middleware and query complexity.
- **Decision**: Keep Company entity (needed for settings, tax%, currency, prefixes) but seed exactly one at startup. No multi-company UI or switcher.
- **Consequences**: Queries skip `companyId` filters; CompanyId foreign keys remain for future upgrade path but queries ignore them for MVP.

## ADR-MVP-004: Remove entities from MVP schema
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: Several entity types add tables/relations with no MVP payoff.
- **Decision**: Remove from MVP: Supplier, Purchase*, Expense* (Expense concept deferred to Phase 2 — no expense tables), LivestockPhoto, Document, Receipt, PaymentAllocation, EmailQueueItem, GeneratedDocument, ApplicationSettings.
- **Consequences**: Smaller migration, fewer configurations, faster build, and a tighter DbContext.

## ADR-MVP-005: Minimal kept schema
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: Need just enough schema to run core livestock sales workflow.
- **Decision**: Keep only: Company, Farm, Customer, Livestock, LivestockWeight, LivestockActivity, Sale, SaleItem, Invoice, InvoiceItem, Payment, SequenceCounter, AuditLog (minimal CreatedBy tracking). Remove InvoiceTax separate table — instead store scalar TaxPercent/TaxAmount on InvoiceItem and TaxTotal on Invoice.
- **Consequences**: Flatter invoice calculation; one less join. Faster to build and test.

## ADR-MVP-006: Authentication without Identity UI RCL
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: Razor Class Library Identity UI pulls in dozens of pages, Area folders, and scaffolding code we do not need.
- **Decision**: Use Identity (UserManager/SignInManager) but skip all Identity UI pages. Roll a simple AccountController with Login / Logout actions. Seed admin user: admin@livestock.dev / Admin@123456.
- **Consequences**: Tighter authentication footprint, easier theming, no password reset pages to maintain in MVP.

## ADR-MVP-007: Payments MVP — single direct payment per invoice
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: PaymentAllocation table and multi-invoice allocation add bookkeeping complexity for zero near-term benefit.
- **Decision**: Each Payment links to one Invoice directly. Invoice status auto-computed: Unpaid / PartiallyPaid / Paid.
- **Consequences**: One-to-many Invoice -> Payments. No allocation UI; simpler status math.

## ADR-MVP-008: Phase 2 deferral list
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: Keep MVP scope tight by deferring everything not on the critical path.
- **Decision**: Email queue, PWA, offline mode, advanced tax, statements, suppliers, purchases — all DEFERRED to Phase 2.
- **Consequences**: MVP 9 slices map directly to critical user path with zero speculative features.

## ADR-MVP-009: Simplify services DI context dependency
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: IAppDbContext + complex unused FluentValidations add abstraction weight with no test benefit right now.
- **Decision**: Services depend directly on AppDbContext via DI. Remove IAppDbContext. Remove FluentValidation validators that are unused (re-add as needed per slice). Keep IUnitOfWork only if it's cheap; otherwise call SaveChanges directly on context.
- **Consequences**: Fewer interfaces, faster navigation through code, minimal abstraction maintenance.

## ADR-MVP-010: Bootstrap 5 responsive sidebar/drawer theme
- **Date**: 2026-08-07
- **Status**: Accepted
- **Context**: lib/ folder already ships Bootstrap 5 locally; want a mobile-friendly layout with agricultural dark-green palette.
- **Decision**: Desktop: left sidebar nav + top nav. Mobile: hamburger icon triggers drawer (offcanvas) sidebar. Dark green agricultural Bootstrap theme.
- **Consequences**: No additional libman/npm installs required. Responsive from slice S3 onward.

