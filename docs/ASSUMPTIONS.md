# ASSUMPTIONS

## Business & Technical Assumptions

This document captures all baseline assumptions for the Livestock Management Invoicing System. Any deviation from these assumptions must be formally approved via the change-control process and recorded in the revision history below.

---

## A1. Currency & Monetary Values

| Item | Assumption |
|---|---|
| **Default Currency** | `USD` is the system-wide default currency. A Company may be configured with `EUR` or `USD` at creation time via `Company.CurrencyCode` (char(3)). Other currencies (GBP, CNY, etc.) are out of scope for v1.0. |
| **Currency Immutability** | Once the first non-draft Invoice or Purchase is posted for a Company, `Company.CurrencyCode` becomes immutable and cannot be changed via UI or API. |
| **Single-Currency Company** | Each company operates in exactly one currency. Multi-currency accounting (FX revaluation, exchange-rate differences) is NOT required in v1.0. All invoices, purchases, payments, and reports for a company share the same `CurrencyCode`. |
| **Money Precision** | All monetary amounts are stored as `decimal(18,2)` in the database. Calculations in memory use `decimal` (28-29 significant digits) to minimize rounding drift, then round to 2 decimals on persist using "round half to even" (banker's rounding). Totals on documents equal the sum of pre-rounded line totals; cross-footing difference ≤ 0.01 accepted with auto-adjustment on last line. |
| **Currency Symbol Display** | Display symbol follows ISO 4217 convention: USD = `$`, EUR = `€`. Symbol position is suffix in EU format (`1.234,56 €`) and prefix in US format (`$1,234.56`), driven by `CulturePreference` on the User entity. |
| **Multi-Company Rollups** | Reports across companies (SystemAdministrator dashboard) show monetary values with a disclaimer: "Values in mixed currencies — not converted." FX conversion for multi-company consolidation is deferred to a future release. |

---

## A2. Tax Rate

| Item | Assumption |
|---|---|
| **Default Tax Rate** | `20%` VAT/sales tax is the system default applied to new Companies via `Company.DefaultTaxRate decimal(5,2) = 20.00`. |
| **Per-Company Configurable** | CompanyAdministrator may override `DefaultTaxRate` in Company Settings within range 0.00 to 35.00. Validation rejects negative rates and rates > 35%. |
| **Per-Document Override** | On individual Purchase, Expense, Sales Order, and Invoice the header-level TaxRate defaults to Company default and may be overridden by the user (Accounts/CompanyAdministrator). Line items inherit header rate unless explicitly set per-line. |
| **No Multi-Tax / Composite Tax** | v1.0 supports one tax rate per document line. Reduced-rate lines, VAT split by jurisdiction, withholding tax, and multi-jurisdiction tax reporting are out of scope. |
| **Tax Rounding** | Tax per line = round(LineTaxable × TaxRate / 100, 2) using banker's rounding. TotalTax = sum(LineTax). Header tax-override recalculates each line. |
| **Tax-Inclusive vs Tax-Exclusive** | Prices are treated as **tax-exclusive** by default. A company-level toggle `PricesIncludeTax bool` (default false) reverses the math: Net = Gross / (1 + Rate/100), Tax = Gross − Net. |

---

## A3. Weight Units

| Item | Assumption |
|---|---|
| **Supported Units** | Only two units of mass are supported: Kilograms (`kg`) and Pounds (`lb`). Other units (tonne, stone, ounce) are out of scope. |
| **Default Weight Unit** | Each Company has `Company.DefaultWeightUnit char(2)` with allowed values `kg` or `lb`. Default at company creation = `kg`. |
| **User Override** | Each User has `User.WeightUnitPreference char(2)` overriding the company default for UI display only. Data entry forms accept both units; user selects unit via radio/dropdown on weight input. |
| **Canonical Internal Storage** | Weights are always stored in the database as **kilograms** using `decimal(18,4)` precision. When a user enters a weight in `lb`, the system converts to kg using the exact factor `1 lb = 0.45359237 kg` before persisting. The originally submitted unit is preserved in `WeightUnit char(2)` for audit/round-trip display. |
| **Display Precision** | Display weights to 4 decimals for animals < 10 kg, 3 decimals for < 100 kg, 2 decimals otherwise. Exported CSVs always emit 4 decimals for downstream analytics. |
| **Conversion at UI Boundary Only** | Business logic (valuation = weight × price, etc.) operates exclusively on the canonical `kg` value to avoid accumulation of conversion errors. |
| **Weight Entry Immutability** | Once a WeightEntry is saved, the WeightKg value and WeightUnit cannot be edited. A correction requires soft-deleting the entry and creating a new one; the reason for correction is recorded in `WeightEntry.Notes`. |

---

## A4. Invoice Payment Terms

| Item | Assumption |
|---|---|
| **Default Due Days** | Invoices default to **30 days** net due date: `DueDate = InvoiceDate + Company.DefaultInvoiceDueDays`. `Company.DefaultInvoiceDueDays int` defaults to `30` at creation; allowed range [0, 180]. |
| **Per-Customer Override** | `Customer.PaymentTermsDays int?` when non-null overrides the company default for new invoices issued to that customer. Existing invoices are unaffected by changing this value. |
| **Per-Invoice Override** | Individual invoice DueDate is user-editable at Draft stage; once Sent, DueDate may only be extended by Accounts role via explicit "Extend Due Date" action that records a note. |
| **Weekend/Holiday Rolling** | Due dates that fall on Saturday/Sunday or public holidays are **not** automatically rolled forward to the next business day. A company-level toggle `DueDateRollToBusinessDay bool` (default false) may be added later. |
| **Overdue Threshold** | An invoice is overdue when `Today > DueDate` (strict inequality — DueDate itself is not overdue). Aging bucket 0-30 days means days-past-due in [0, 30], etc. |
| **Reminders** | Automatic overdue reminder emails are not sent in v1.0. A "Send Reminder" manual action is available per invoice (Accounts role). |

---

## A5. Livestock Type Codification

| Item | Assumption |
|---|---|
| **Fixed 2-Letter Codes** | Livestock.Type is a char(2) enum-like field with exactly five allowed values: |
| | `Ah` = **Aves / Poultry** (chickens, turkeys, ducks, geese, and other fowl) |
| | `Su` = **Swine / Pigs** (domestic pigs, boars, sows, piglets) |
| | `Sa` = **Sheep** (ewes, rams, lambs) |
| | `Ad` = **Adult Bovine** (cattle over 12 months: cows, bulls, oxen, heifers >1yr) |
| | `Sd` = **Young Bovine / Calves** (cattle under 12 months) |
| **Codes Are Database Constants** | The 5 codes are not user-editable. New species (equine, caprine, etc.) require a schema-level release. |
| **Sub-classification** | Sub-species/breeds are free-text in `Livestock.Breed` (nvarchar(128)) rather than a separate lookup in v1.0. |
| **Age Cohort Transitions** | The boundary between `Sd` (young bovine) and `Ad` (adult bovine) is informational at creation time. The system does NOT automatically retype livestock when age crosses 12 months; a manual "Promote to Adult" action (FarmManager+) records the transition with timestamp. |
| **Default Weights By Type** | Seed data provides a recommended default initial weight per type (e.g. Ah=0.0500 kg chick, Su=2.0 kg piglet, Sa=5.0 kg lamb, Sd=40.0 kg calf, Ad=450.0 kg cow) that auto-fills the New Livestock form — user overridable. |

---

## A6. Document Sequence Counters

| Item | Assumption |
|---|---|
| **Scope** | All document number sequences are scoped **per Company** and reset **per Calendar Year**. A single global sequence per document type across all companies is explicitly NOT used (prevents number gaps when one company is audited and another issues docs). |
| **Document Types with Sequences** | 6 counters per company: `Purchase`, `Expense` (optional if simple), `SalesOrder`, `Invoice`, `Payment`, `Receipt`. |
| **Format** | `YYYY-NNNNN` where `YYYY` is the 4-digit year of the document date (NOT the created-at year) and `NNNNN` is a 5-digit zero-padded integer starting at `00001` each January 1. |
| **Gaps & Voided Documents** | Numbers are consumed at document creation (Draft status). Voiding a document does NOT reclaim the number (required by most tax authorities — gap-less sequences with voided documents retained in audit trail). |
| **Atomic Increment** | Counters are incremented inside a `SERIALIZABLE` isolation-level transaction (or via SQL `UPDATE SequenceCounter SET NextValue = NextValue + 1 OUTPUT ...`) so concurrent creations under load never produce duplicates. |
| **Year-Roll Logic** | On first document created each year, the sequence automatically seeds at 1 for that year. If an admin back-dates a document into a prior closed year (allowed only by CompanyAdministrator with audit flag), a separate counter bucket for that historical year is created/used. |
| **Manual Override** | Document numbers are NOT manually editable except by SystemAdministrator via a deliberate "Set Custom Number" flow (logged to audit with reason). This is a break-glass for migration from a legacy system. |

---

## A7. Development Users — Production Guard

| Item | Assumption |
|---|---|
| **Dev Seed Users** | In Development environments the system seeds a canonical set of 6 test users (one per role: Viewer, DataEntry, FarmManager, Accounts, CompanyAdministrator, SystemAdministrator) with well-known emails `{role}@example.com` and password `Passw0rd!`. |
| **Production Blocking** | These dev seed users shall **NEVER** be enabled, created, or usable in Production. The seed method is guarded by TWO independent conditions that MUST BOTH be true: |
| | **Guard 1 (Environment):** `ASPNETCORE_ENVIRONMENT == "Development"` (string comparison, ordinal ignore-case). |
| | **Guard 2 (Explicit Opt-in Switch):** Environment variable `ENV_ENABLE_DEV_SEED == "true"` (or `1`, or `yes` — case-insensitive parse). |
| | If either guard is false, `SeedDevUsersAsync()` returns immediately without creating or modifying any users. |
| **Staging Also Blocked by Default** | Since Staging does not set `ASPNETCORE_ENVIRONMENT=Development`, dev users are not seeded in Staging unless the deployment engineer explicitly sets both environment variables (strongly discouraged). |
| **Additional Sanity Check** | On every `DbContext.SaveChangesAsync()` for User insert, a runtime check rejects inserting any user whose email ends with `@example.com` when `ASPNETCORE_ENVIRONMENT == "Production"`. |
| **Password Rotation** | Dev seed users are created with a forced password-change-on-next-login flag so that even if they were ever accidentally accessible, the first login would force a reset to a non-default password. |

---

## A8. Supplementary Assumptions (General)

| Item | Assumption |
|---|---|
| **Single Time Zone per Company** | Each company operates in a single IANA time zone stored in `Company.TimeZoneId` (default `"UTC"`). All user-facing dates are displayed in this zone; the database stores everything as `DateTimeOffset` with the offset captured at write time. |
| **Backup Retention** | Database backup retention is 7 daily backups + 4 weekly + 12 monthly. File/blob storage backups mirror this policy. |
| **Email Deliverability** | System uses one SMTP/SendGrid account per deployment (not per company). Emails are sent `From: {Company.SenderEmail || noreply@system.com}` with `Reply-To:` set to the company's contact email. DKIM/SPF configuration is the tenant's responsibility. |
| **PDF Rendering** | Invoices use QuestPDF (or equivalent) rendering engine. Non-Latin glyphs require additional font packages configured on the host. |
| **Concurrency** | Optimistic concurrency via a `Version` rowversion/timestamp column. Concurrent edits to the same invoice return a friendly "this record was modified by another user" message; user reloads and re-applies changes. |
| **Soft-Delete Default** | `IsDeleted` defaults to 0 (false). Queries exclude deleted unless explicitly requested by SystemAdministrator running an "Include Deleted" audit report. |
| **Attachments Storage Quota** | Default per-Company storage quota: 50 GB. Quota warning at 80%, hard cap at 100 GB. Admin can increase per company. |

---

## Revision History

| Date | Version | Author | Change Description |
|---|---|---|---|
| 2026-08-06 | 1.0 | Spec Author | Initial assumptions baseline. |
