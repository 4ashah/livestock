# REQUIREMENTS

## Functional Requirements — Livestock Management Invoicing System

All requirements below are grouped by module. Each requirement carries a stable identifier (`REQ-<MODULE>-<NNN>`) used in the traceability matrix.

---

## Module 1: Companies & Farms

### Company Management

| ID | Requirement | Priority |
|---|---|---|
| REQ-CF-001 | System shall allow a SystemAdministrator to create a Company record with Name, RegistrationNumber (VAT/Tax ID), LegalAddress, ContactEmail, ContactPhone, CurrencyCode (USD/EUR), DefaultTaxRate, DefaultWeightUnit (kg/lb), DefaultInvoiceDueDays, Logo blob/url, and IsActive flag. | MUST |
| REQ-CF-002 | System shall assign every user (except SystemAdministrator) to exactly one Company via a non-nullable `CompanyId` FK on the User entity. | MUST |
| REQ-CF-003 | System shall enforce that data queries always filter by the current user's `CompanyId` via a global EF Core query filter so that cross-company data leakage is impossible at the ORM level. | MUST |
| REQ-CF-004 | System shall allow CompanyAdministrator to edit their own company profile fields excluding `CurrencyCode` (immutable after first invoice is posted). | MUST |
| REQ-CF-005 | System shall soft-delete companies (set `IsDeleted = 1`) rather than hard-delete. System shall block deletion of a company with any non-draft invoices. | MUST |
| REQ-CF-006 | System shall provide a sequence counter per company per document type (PurchaseNumber, SalesOrderNumber, InvoiceNumber, ReceiptNumber, PaymentNumber). Sequences shall reset yearly on Jan 1, format `YYYY-NNNNN`. | MUST |
| REQ-CF-007 | System shall generate the next document number atomically using a database transaction so concurrent requests never produce duplicates. | MUST |

### Farm Management

| ID | Requirement | Priority |
|---|---|---|
| REQ-CF-101 | System shall allow CompanyAdministrator / FarmManager to create, update, and soft-delete Farms: Name, Code (unique per company), Address, Region, ManagerUserId (FK), AreaHa (optional), GPSCoords (optional), Notes, IsActive. | MUST |
| REQ-CF-102 | System shall enforce that every Livestock record must reference an existing Farm via non-null `FarmId` FK. | MUST |
| REQ-CF-103 | System shall list farms filtered by company; viewer users shall have read-only access. | MUST |
| REQ-CF-104 | System shall prevent soft-delete of a Farm if any active (alive) livestock are linked to it, unless the caller confirms re-assignment to another farm. | SHOULD |

---

## Module 2: Livestock

### Livestock Registry

| ID | Requirement | Priority |
|---|---|---|
| REQ-LV-001 | System shall allow FarmManager / DataEntry to create Livestock with: TagId (unique per company, up to 64 chars), LivestockType enum (Ah, Su, Sa, Ad, Sd), DOB, Gender enum (Male/Female/Unknown), Breed (string, optional), InitialWeight decimal(18,4), InitialWeightUnit (kg/lb), FarmId FK, SireTagId (optional self-FK), DamTagId (optional self-FK), Notes, and photo attachment. | MUST |
| REQ-LV-002 | LivestockType enum: `Ah` = Purchased castrated ram, `Su` = Uncastrated ram, `Sa` = Purchased ewe, `Ad` = Bred castrated ram, `Sd` = Bred ewe. All five codes are ovine (sheep) classifications only. Purchased types (Ah, Su, Sa) require PurchaseAmount > 0; Bred types (Ad, Sd) require PurchaseAmount == 0 exactly. System shall display localized labels; database stores the 2-letter code as char(2). | MUST |
| REQ-LV-003 | System shall store weights internally as `decimal(18,4) kg` in a canonical form, preserving the original submitted unit in `WeightUnit` field for display. Conversion factor: 1 lb = 0.45359237 kg exact. | MUST |
| REQ-LV-004 | System shall support multiple WeightEntry records per Livestock (WeighedAt DateTimeOffset, Weight decimal(18,4), WeightUnit, WeighedByUserId). Weight entries shall be immutable once saved (no update, only soft-delete). | MUST |
| REQ-LV-005 | System shall compute current weight as the most recent non-deleted WeightEntry, falling back to InitialWeight. | MUST |
| REQ-LV-006 | System shall enforce LivestockStatus lifecycle: `Alive` → `Sold` / `Slaughtered` / `Dead`. Transitions are irreversible; status change must record timestamp and user. | MUST |
| REQ-LV-007 | System shall automatically set `Status = Sold` on livestock linked to an Invoice that reaches status `Paid` (or `Sent` configurable). `SoldAt` timestamp and `SaleId` FK populated. | MUST |
| REQ-LV-008 | System shall support batch-create of livestock: import CSV with columns TagId,Type,DOB,Gender,Breed,Weight,Unit,FarmCode. Import validates each row, rejects entire batch on N>0 invalid rows with per-row errors. | SHOULD |
| REQ-LV-009 | Livestock list page supports filters: Farm, LivestockType, Status, TagId partial match, DOB date range, Created date range, min/max current weight. Supports server-side pagination and CSV export. | MUST |

---

## Module 3: Customers & Suppliers

### Customers

| ID | Requirement | Priority |
|---|---|---|
| REQ-CS-001 | System shall allow Accounts / DataEntry to create Customers: Code (unique per company), Name, LegalName, TaxId (VAT/CIF optional), BillingAddress, ShippingAddress, Email, Phone, PaymentTermsDays (default 30 inherited from company), CreditLimit decimal(18,2) nullable, CurrencyCode inherited from company, Notes, IsActive. | MUST |
| REQ-CS-002 | Customer list supports: Name search, active-only toggle, sort by balance due descending, CSV export. | MUST |
| REQ-CS-003 | Customer detail page shows: total invoiced, total paid, current balance, aged balance buckets (0-30, 31-60, 61-90, 90+), last 10 invoices. | SHOULD |
| REQ-CS-004 | System shall prevent soft-delete of a Customer if any unpaid invoices exist. | MUST |

### Suppliers

| ID | Requirement | Priority |
|---|---|---|
| REQ-CS-101 | System shall allow Accounts / DataEntry to create Suppliers: Code (unique per company), Name, LegalName, TaxId, Address, Email, Phone, BankAccount (IBAN/ACC), PaymentTermsDays, CurrencyCode, Notes, IsActive. | MUST |
| REQ-CS-102 | Supplier detail page shows: total purchased, total paid, current balance, aged A/P buckets, last 10 purchases. | SHOULD |
| REQ-CS-103 | System shall prevent soft-delete of a Supplier if any unpaid purchase bills exist. | MUST |

---

## Module 4: Purchases & Expenses

### Purchases (Livestock Intake)

| ID | Requirement | Priority |
|---|---|---|
| REQ-PE-001 | System shall create Purchase documents: PurchaseNumber (company-scoped YY-NNNNN), SupplierId FK, PurchaseDate, FarmId (destination farm), Status enum (Draft/Posted/Voided), TaxRate decimal(5,2) default from company. | MUST |
| REQ-PE-002 | Purchase lines (one to many): LineNo, LivestockType, Quantity int, UnitWeight decimal(18,4), WeightUnit, UnitPrice decimal(18,2), DiscountPct decimal(5,2) default 0, TaxRate decimal(5,2) inherited from header, LineTotal computed. | MUST |
| REQ-PE-003 | Purchase totals: SubTotal = sum(LineTotal), TotalDiscount, TaxableAmount, TotalTax, GrandTotal. User may override header-level DiscountPct and TaxRate; lines recalculate. | MUST |
| REQ-PE-004 | Purchase **Posted** status shall be irreversible. On Post, system atomically creates N Livestock records per line (Qty rows) with shared TagIds pattern `{PurchaseNumber}-{LineNo}-{Seq}` (or accepts user-provided list if TagId list supplied) plus one WeightEntry each. | MUST |
| REQ-PE-005 | Purchases can have file attachments (purchase invoices/receipts): allow-list `.pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx,.csv`, max 10 MB per file. | MUST |
| REQ-PE-006 | Purchase list page filters: Supplier, Date range, Status, Keyword search. Supports CSV export. | MUST |

### Expenses

| ID | Requirement | Priority |
|---|---|---|
| REQ-PE-101 | System shall allow Accounts / DataEntry to create Expense records: ExpenseDate, ExpenseCategory enum (Feed, Vet, Transport, Utilities, Labor, Other), SupplierId FK (optional), FarmId FK (optional), Amount decimal(18,2), TaxRate default company, TaxAmount, GrandTotal, PaymentMethod enum (Cash, BankTransfer, Check, Card), Notes, receipt file attachment. | MUST |
| REQ-PE-102 | Expense categories support user-defined extensions via a Company-scoped lookup table. Default 6 categories always present. | SHOULD |
| REQ-PE-103 | Expense list filters: Category, Date range, Farm, Supplier, >Amount threshold. CSV + Excel export. | MUST |

---

## Module 5: Sales & Invoicing

### Sales Orders

| ID | Requirement | Priority |
|---|---|---|
| REQ-SI-001 | System shall allow Accounts / DataEntry to create Sales Orders: SalesOrderNumber (company-scoped), CustomerId FK, OrderDate, FarmId, Status (Draft/Confirmed/Cancelled/Invoiced), Notes. | MUST |
| REQ-SI-002 | Sales Order lines reference existing Livestock records by TagId OR generic batch line (LivestockType + Quantity). Each line: UnitPrice, DiscountPct, LineTotal. | MUST |
| REQ-SI-003 | Total calculations mirror purchase: SubTotal, TotalDiscount, TaxableAmount, TotalTax, GrandTotal. | MUST |
| REQ-SI-004 | Only Confirmed sales orders may generate invoices. Invoiced order cannot be modified; cancelling an order requires linked invoice be voided first. | MUST |

### Invoicing

| ID | Requirement | Priority |
|---|---|---|
| REQ-SI-101 | Invoice model: InvoiceNumber (YY-NNNNN), CustomerId FK, InvoiceDate, DueDate (default InvoiceDate + Company.DefaultInvoiceDueDays, default 30 days), Status enum (Draft/Sent/Paid/Voided), SalesOrderId FK (nullable for direct invoices), BillingAddress (snapshot), ShippingAddress (snapshot), CurrencyCode (snapshot), TaxRate (snapshot). | MUST |
| REQ-SI-102 | Invoice lines snapshot: LineNo, Description, Quantity, UnitWeight snapshot, WeightUnit snapshot, UnitPrice snapshot, DiscountPct snapshot, TaxRate snapshot, LineTotal snapshot. Snapshots are immutable — editing customer/product fields must never revise already-printed invoices. | MUST |
| REQ-SI-103 | Invoice totals: SubTotal, TotalDiscount, TaxableAmount, TotalTax, GrandTotal, AmountPaid (derived from Payment allocations), BalanceDue = GrandTotal − AmountPaid. | MUST |
| REQ-SI-104 | System shall generate PDF invoices on demand (synchronous for UI download, async for email queue). PDF layout: Company letterhead (logo+address), Customer billing address, InvoiceNumber/Date/DueDate, line items table, totals block, bank info, footer. | MUST |
| REQ-SI-105 | Invoice status transitions: `Draft → Sent` (email queued + PDF generated); `Sent → Paid` (full payment received); `Draft/Sent → Voided` (requires Accounts role, records VoidReason and VoidedBy). Voided invoice must not affect totals; livestock status revert logic configurable (default: revert to Alive if sold). | MUST |
| REQ-SI-106 | Email invoice: send to Customer.Email with invoice PDF attachment, subject `Invoice {InvoiceNumber} from {Company.Name}`, customizable template per company. Uses background queue; retries 3×; logs SendResult. | MUST |
| REQ-SI-107 | System shall flag invoices overdue when `Today > DueDate AND BalanceDue > 0`. Overdue status computed live (not persisted). Aging bucket 31-60/61-90/90+ calculated from (Today − DueDate). | MUST |
| REQ-SI-108 | Invoice list page filters: Customer, Status, Date range (InvoiceDate or DueDate), overdue-only, Keyword (number/customer name). Columns: InvoiceNumber, Customer, InvoiceDate, DueDate, GrandTotal, AmountPaid, BalanceDue, Status, OverdueBadge. Bulk-send-in-email action. | MUST |
| REQ-SI-109 | Recurring invoices (monthly retainer-style): template saved with lines; system generates Draft invoice on 1st of each month via Hangfire job per company's opt-in config. | COULD |

---

## Module 6: Payments & Receipts

### Customer Payments

| ID | Requirement | Priority |
|---|---|---|
| REQ-PR-001 | System shall record Customer Payments: PaymentNumber, CustomerId FK, PaymentDate, PaymentMethod (Cash/BankTransfer/Check/Card/Online), ReferenceNo (check no / transaction id), Amount decimal(18,2), DepositedAccount, Notes, CreatedByUserId. | MUST |
| REQ-PR-002 | Payments shall be allocated to Invoices: default strategy = "oldest due first" (allocate to earliest DueDate unpaid balance until Amount exhausted). Manual allocation: user selects invoice rows and enters allocation amounts; total allocations must equal Amount. | MUST |
| REQ-PR-003 | Over-payments: when allocated total < Amount, remainder creates a CreditNote (negative invoice) against customer, usable for future allocations. | MUST |
| REQ-PR-004 | When Invoice.BalanceDue reaches 0 (within tolerance 0.01), status auto-flips to Paid; if Invoice was linked to livestock, Livestock.Status = Sold (if not already). | MUST |
| REQ-PR-005 | Payment reversal: voiding a payment reverses allocations, credits back AmountPaid on affected invoices, restores livestock status to Alive if no other paid invoice covers it. | SHOULD |
| REQ-PR-006 | Payment list filters: Customer, PaymentMethod, Date range, Reference search. Excel export. | MUST |

### Supplier Receipts (Payments Out)

| ID | Requirement | Priority |
|---|---|---|
| REQ-PR-101 | System shall record Supplier Receipts (payments to suppliers): ReceiptNumber, SupplierId FK, PaymentDate, PaymentMethod, ReferenceNo, Amount, BankAccountOut, Notes. Allocate to Purchases (Posted status) using oldest-first or manual strategy. | MUST |
| REQ-PR-102 | Supplier Balance = Sum(Purchase.Posted GrandTotal) − Sum(Receipt Allocated). Displayed on Supplier detail and A/P Aging report. | MUST |

---

## Module 7: Reports

| ID | Requirement | Priority |
|---|---|---|
| REQ-RP-001 | Dashboard KPIs (all company-scoped, period = current month MTD default, user-selectable period): (a) Total Sales, (b) Total Purchases, (c) Total Expenses, (d) Net Profit = Sales − Purchases − Expenses, (e) Receivables (sum of BalanceDue > 0), (f) Payables, (g) Livestock Alive count, (h) Overdue Invoices count. | MUST |
| REQ-RP-002 | **Sales by Period Report** — parameters: Date range, GroupBy (Day/Week/Month), Customer (optional), Farm (optional). Columns: Period, Invoices Count, SubTotal, Discount, Tax, Total, Paid, Unpaid. Chart: line/bar. | MUST |
| REQ-RP-003 | **Purchases by Period Report** — parameters: Date range, GroupBy, Supplier (optional), Farm (optional). Analogous columns to sales report. | MUST |
| REQ-RP-004 | **Livestock Valuation Report** — per-Farm and per-Type subtotals. Valuation = Livestock.CurrentWeight × AvgUnitPrice (weighted-average last 12 months purchases). Columns: Farm, Type, HeadCount, TotalWeight kg, AvgUnitPrice, EstimatedValue. | MUST |
| REQ-RP-005 | **A/R Aging Report (Accounts Receivable)** — one row per Customer with balance > 0. Columns: Customer, Current (<=DueDate), Bucket30, Bucket60, Bucket90, Over90, TotalBalance. Hyperlink to customer detail. | MUST |
| REQ-RP-006 | **A/P Aging Report (Accounts Payable)** — analogous per Supplier for unpaid purchases. | MUST |
| REQ-RP-007 | **Profit & Loss Summary** — Date range. Sections: Revenue (Sales Total), COGS (Purchases Total), Gross Profit = Revenue − COGS, Operating Expenses (Expenses grouped by category), Operating Profit, Net Profit. | MUST |
| REQ-RP-008 | **Expense by Category Report** — Date range. Pie chart + tabular breakdown. | MUST |
| REQ-RP-009 | Every report supports (a) on-screen HTML table, (b) CSV export (UTF-8 BOM, RFC4180), (c) Excel export (xlsx, multiple sheets if applicable). All exports use current filter parameters. | MUST |
| REQ-RP-010 | Reports are accessible to Viewer+ role. Export to file requires DataEntry+ role (configurable policy). | MUST |

---

## Module 8: User Interface

| ID | Requirement | Priority |
|---|---|---|
| REQ-UI-001 | Web UI is an ASP.NET Core MVC + Razor Pages app with a responsive layout (mobile/tablet/desktop) using Bootstrap 5. Sidebar navigation with collapsible menu per role; top bar with company logo switcher (SystemAdministrator only), user menu (profile/logout), dark/light theme toggle. | MUST |
| REQ-UI-002 | Login page with username/email + password, remember-me, forgot-password reset flow (email token), TOTP 2FA opt-in per user. Lockout after 5 failed attempts for 5 minutes. | MUST |
| REQ-UI-003 | Form validation: both client-side (jQuery unobtrusive) and server-side (FluentValidation) with consistent error display. Required fields marked with red asterisk. | MUST |
| REQ-UI-004 | All list pages use jQuery DataTables with server-side processing for tables expected to exceed 1,000 rows (Livestock, Invoices, Payments). Client-side for small lookups. | MUST |
| REQ-UI-005 | Master-Detail pattern: header CRUD page + inline editable line items grid for Purchases, Sales Orders, Invoices, Payments. Line add/remove without full page reload via unobtrusive AJAX or small JS component. | MUST |
| REQ-UI-006 | Toast notifications (success/info/warning/error) via TempData + Bootstrap Toasts on every POST redirect. | MUST |
| REQ-UI-007 | Localization: UI strings in resx files, default en-US; Currency symbol and date formats follow Company.CurrencyCode and User's CulturePreference. | SHOULD |
| REQ-UI-008 | Accessibility: WCAG 2.1 AA — semantic HTML, labels on all inputs, color-contrast ≥ 4.5:1 for body text, keyboard-navigable, ARIA roles on dynamic lists. | SHOULD |
| REQ-UI-009 | Global search bar in top nav: searches TagId, InvoiceNumber, PurchaseNumber, Customer/Supplier Name, returns categorized results scoped to company. | COULD |
| REQ-UI-010 | File upload widget with drag-drop, preview, progress bar, validation of extension + size client-side before upload. | MUST |

---

## Module 9: Security

| ID | Requirement | Priority |
|---|---|---|
| REQ-SE-001 | Authentication: ASP.NET Core Identity with extended `ApplicationUser`. Password policy: min length 10, require non-alphanumeric, uppercase, lowercase, digit. Lockout on 5 failures. | MUST |
| REQ-SE-002 | Authorization: 6 roles (Viewer, DataEntry, FarmManager, Accounts, CompanyAdministrator, SystemAdministrator). Every page/API uses `[Authorize(Policy = "Xxx")]` policy-based auth; policies aggregate role requirements. Policy table defined in SECURITY.md. | MUST |
| REQ-SE-003 | CSRF protection: ASP.NET Core Antiforgery enabled globally for all non-GET requests; all forms include request verification token; AJAX requests pass token via header. | MUST |
| REQ-SE-004 | XSS protection: All untrusted user input HTML-encoded by default in Razor; rich text (if ever added) sanitized via HtmlSanitizer library with strict allow-list; Content-Security-Policy header. | MUST |
| REQ-SE-005 | SQL Injection: All queries use EF Core LINQ / parameterized queries; raw SQL uses `FromSqlInterpolated` with interpolated parameters. Never string concat. | MUST |
| REQ-SE-006 | File upload hardening: allow-list of extensions `.pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx,.csv`; max size 10 MB per file; files stored by content-derived GUID filename; extension verified against actual file signature (magic bytes) for images and PDFs; MIME type sniffing disabled via server headers. | MUST |
| REQ-SE-007 | Company isolation: global EF Core query filter on every entity with `CompanyId`; `SaveChangesAsync` override rejects inserts/updates where `CompanyId` differs from current user's company (except SystemAdministrator with explicit company switch). | MUST |
| REQ-SE-008 | Audit logging: every Create/Update/Delete of auditable entities writes row to AuditLogs table (UserId, CompanyId, EntityType, EntityId, Operation, OldValues JSON, NewValues JSON, Timestamp UTC). Query reads not logged by default. | MUST |
| REQ-SE-009 | Data protection: API keys and connection strings stored in environment variables / Azure KeyVault, never in `appsettings.json` repo. `appsettings.Production.json` excluded from repo via `.gitignore`. | MUST |
| REQ-SE-010 | Development users seed (default admin `admin@example.com` / `Passw0rd!`, 5 other role test users) shall NEVER be created in Production. Seed runs ONLY when `ASPNETCORE_ENVIRONMENT == Development` AND `ENV_ENABLE_DEV_SEED == true`. Both conditions required; explicit opt-in env switch as defense in depth. | MUST |
| REQ-SE-011 | HTTPS: enforced HSTS header with 1-year max-age; production site redirects HTTP→HTTPS; cookies `Secure`, `HttpOnly`, `SameSite=Lax`. | MUST |
| REQ-SE-012 | Rate limiting: login endpoint 10 req/min per IP; password reset 5 req/hour per email; general API 500 req/min per user. | SHOULD |
| REQ-SE-013 | Session: absolute expiry 12 hours; sliding expiry 30 minutes idle. Idle timeout forces re-login. | MUST |
