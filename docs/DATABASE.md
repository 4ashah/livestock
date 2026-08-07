# DATABASE SCHEMA

## Livestock Management Invoicing System — SQL Server

### Conventions used in this document

- **PK** = Primary Key (all PKs are `uniqueidentifier`, clustered or non-clustered + separate `bigint IDENTITY` surrogate cluster key optional).
- **FK** = Foreign Key; naming: `FK_Table_ReferencedTable_Column`.
- **UK** = Unique Key/Constraint; naming: `UK_Table_Column(s)`.
- **IX** = Index; naming: `IX_Table_Column(s)`.
- Money columns: `decimal(18,2)`.
- Weight columns: `decimal(18,4)`.
- Percent columns (TaxRate, DiscountPct): `decimal(5,2)` (range 0.00–999.99; validation ensures ≤ 35.00 for tax).
- DateTime columns: all persisted as **UTC** via `DateTimeOffset(7)`. App code never writes local `DateTime` without offset.
- Every entity row has `IsDeleted bit NOT NULL DEFAULT(0)`, `CreatedAt datetimeoffset(7) NOT NULL`, `ModifiedAt datetimeoffset(7) NOT NULL`, `Version rowversion NOT NULL` (concurrency token).
- All tables live in schema `dbo` except ASP.NET Core Identity tables (default `dbo` prefixed `AspNet*`) and Hangfire (schema `hangfire`).

---

## 1. Companies & Farms

### `Companies`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK (clustered) | |
| `Name` | nvarchar(200) | NO | | IX, UK partial (with IsDeleted) | Display + legal name |
| `RegistrationNumber` | nvarchar(50) | YES | NULL | IX | VAT/Tax ID |
| `LegalAddress_Line1` | nvarchar(200) | NO | | | Owned entity columns |
| `LegalAddress_Line2` | nvarchar(200) | YES | NULL | | |
| `LegalAddress_City` | nvarchar(100) | NO | | | |
| `LegalAddress_Region` | nvarchar(100) | YES | NULL | | |
| `LegalAddress_PostalCode` | nvarchar(20) | NO | | | |
| `LegalAddress_CountryCode` | char(2) | NO | N'US' | | ISO 3166-1 |
| `ContactEmail` | nvarchar(254) | YES | NULL | | |
| `ContactPhone` | nvarchar(30) | YES | NULL | | |
| `CurrencyCode` | char(3) | NO | N'USD' | CHECK IN ('USD','EUR') | Immutable after 1st posted invoice |
| `DefaultTaxRate` | decimal(5,2) | NO | 20.00 | CHECK >=0 AND <=35 | |
| `DefaultWeightUnit` | char(2) | NO | N'kg' | CHECK IN ('kg','lb') | |
| `DefaultInvoiceDueDays` | int | NO | 30 | CHECK BETWEEN 0 AND 180 | |
| `PricesIncludeTax` | bit | NO | 0 | | |
| `TimeZoneIanaId` | nvarchar(64) | NO | N'UTC' | | |
| `PaperSize` | char(2) | NO | N'A4' | CHECK IN ('A4','LT') | |
| `LogoStoredFileId` | uniqueidentifier | YES | NULL | FK -> StoredFiles.Id ON DELETE SET NULL | |
| `SenderEmail` | nvarchar(254) | YES | NULL | | From-address on invoices |
| `BankAccountName` | nvarchar(128) | YES | NULL | | Printed on invoice |
| `BankAccountIban` | nvarchar(34) | YES | NULL | | |
| `BankAccountSwift` | nvarchar(11) | YES | NULL | | |
| `StorageQuotaGb` | decimal(9,2) | NO | 50.00 | CHECK >=0 | |
| `IsActive` | bit | NO | 1 | | |
| `IsDeleted` | bit | NO | 0 | | Soft delete |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `Version` | rowversion | NO | auto | | Concurrency token |

- **UK**: `UK_Companies_Name_IsDeleted` unique filtered on `IsDeleted = 0` (duplicate names not allowed for active companies).
- **FK constraint**: Prevent `CurrencyCode` change via trigger or check UDF once any Invoice/SalesOrder/Purchase row with Status > Draft exists for this CompanyId.

### `Farms`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK -> Companies.Id ON DELETE CASCADE, IX (compound) | |
| `Name` | nvarchar(200) | NO | | | |
| `Code` | nvarchar(32) | NO | | UK (CompanyId + Code + !IsDeleted) | Human-readable short code |
| `Address_Line1` | nvarchar(200) | YES | NULL | | |
| `Address_City` | nvarchar(100) | YES | NULL | | |
| `Address_Region` | nvarchar(100) | YES | NULL | | |
| `Address_PostalCode` | nvarchar(20) | YES | NULL | | |
| `Address_CountryCode` | char(2) | YES | N'US' | | |
| `ManagerUserId` | uniqueidentifier | YES | NULL | FK -> AspNetUsers.Id ON DELETE SET NULL | |
| `AreaHa` | decimal(10,4) | YES | NULL | CHECK >= 0 | Hectares |
| `GpsLatitude` | decimal(9,6) | YES | NULL | CHECK BETWEEN -90 AND 90 | |
| `GpsLongitude` | decimal(9,6) | YES | NULL | CHECK BETWEEN -180 AND 180 | |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `IsActive` | bit | NO | 1 | | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `Version` | rowversion | NO | auto | | Concurrency token |

- **IX_Farms_CompanyId_IsActive**: nonclustered (used by farm lookup queries).

---

## 2. Identity Tables (Extended)

### `AspNetUsers` (extended via EF Core migration)

Standard ASP.NET Identity columns PLUS:

| Column (additional) | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `CompanyId` | uniqueidentifier | YES | NULL | FK -> Companies.Id ON DELETE SET NULL, IX | NULL only for SystemAdministrator |
| `FullName` | nvarchar(128) | NO | N'' | | |
| `DisplayName` | nvarchar(64) | YES | NULL | | |
| `TimeZoneIanaId` | nvarchar(64) | NO | N'UTC' | | |
| `CultureCode` | nvarchar(10) | NO | N'en-US' | | |
| `WeightUnitPreference` | char(2) | YES | NULL | CHECK IN ('kg','lb') | NULL = inherit Company default |
| `IsEnabled` | bit | NO | 1 | | Administrative lock-out (separate from Identity lockout) |
| `LastLoginAt` | datetimeoffset(7) | YES | NULL | | |
| `PasswordChangedAt` | datetimeoffset(7) | YES | NULL | | |
| `ForcePasswordChange` | bit | NO | 0 | | |
| `IsDeleted` | bit | NO | 0 | | Soft delete (do not remove rows) |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `Version` | rowversion | NO | auto | | Concurrency token |

### `AspNetRoles` (extended)

Standard Identity columns +:

| Column (additional) | Type | Null | Default | Notes |
|---|---|---|---|---|
| `Description` | nvarchar(500) | YES | NULL | Human-readable purpose |
| `Rank` | smallint | NO | 100 | Hierarchy rank; higher = more privileged (used for "at least X role" checks) |

Seeded roles:

| Role NormalizedName | Description | Rank |
|---|---|---|
| VIEWER | Read-only access to most data; cannot modify or export. | 10 |
| DATAENTRY | Can create/edit livestock, purchases, expenses; cannot void invoices. | 30 |
| FARMMANAGER | Can manage livestock and farms; full livestock lifecycle permissions. | 50 |
| ACCOUNTS | Can manage sales, invoices, payments/receipts; can void invoices. | 70 |
| COMPANYADMINISTRATOR | Full control within one company; user/role management within company. | 90 |
| SYSTEMADMINISTRATOR | Cross-company access; system configuration; highest privilege. | 120 |

### Additional Identity Tables (standard)

`AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens` — all default Microsoft Identity schema, no custom columns.

### `UserRoleAssignmentAudits`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK (clustered) | |
| `UserId` | uniqueidentifier | NO | | FK -> AspNetUsers.Id | |
| `RoleId` | uniqueidentifier | NO | | FK -> AspNetRoles.Id | |
| `Action` | char(1) | NO | | CHECK IN ('G','R') | Granted / Revoked |
| `ByUserId` | uniqueidentifier | NO | | FK -> AspNetUsers.Id | |
| `At` | datetimeoffset(7) | NO | sysdatetimeoffset() | IX | |
| `Reason` | nvarchar(256) | YES | NULL | | |

---

## 3. Livestock Module

### `Livestock`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX (compound with Type/Status/IsDeleted) | |
| `FarmId` | uniqueidentifier | NO | | FK -> Farms.Id | |
| `TagId` | nvarchar(64) | NO | | UK (CompanyId + TagId + !IsDeleted) | Ear tag / visual ID |
| `LivestockType` | char(2) | NO | | CHECK IN ('Ah','Su','Sa','Ad','Sd'), IX | |
| `Status` | tinyint | NO | 0 | CHECK IN (0,1,2,3) | 0=Alive, 1=Sold, 2=Slaughtered, 3=Dead |
| `DOB` | date | YES | NULL | IX | |
| `Gender` | char(1) | NO | N'U' | CHECK IN ('M','F','U') | Male/Female/Unknown |
| `Breed` | nvarchar(128) | YES | NULL | | |
| `InitialWeightKg` | decimal(18,4) | NO | | CHECK >=0 | Canonical storage in kg |
| `InitialWeightUnit` | char(2) | NO | N'kg' | CHECK IN ('kg','lb') | Original unit entered |
| `CurrentWeightKg` | decimal(18,4) | NO | | CHECK >=0 | Denormalized from latest WeightEntry (maintained by trigger/handler) |
| `CurrentWeightAsAt` | datetimeoffset(7) | YES | NULL | | Timestamp of weight used |
| `SireLivestockId` | uniqueidentifier | YES | NULL | FK -> Livestock.Id ON DELETE SET NULL | Parent sire |
| `DamLivestockId` | uniqueidentifier | YES | NULL | FK -> Livestock.Id ON DELETE SET NULL | Parent dam |
| `OriginPurchaseId` | uniqueidentifier | YES | NULL | FK -> Purchases.Id ON DELETE SET NULL | Batch source |
| `SoldSaleId` | uniqueidentifier | YES | NULL | FK -> Invoices.Id ON DELETE SET NULL | Linked sale |
| `SoldAt` | datetimeoffset(7) | YES | NULL | | |
| `DeceasedAt` | datetimeoffset(7) | YES | NULL | | Set when Status in (2,3) |
| `DeceasedCause` | nvarchar(256) | YES | NULL | | |
| `SlaughterBatchRef` | nvarchar(64) | YES | NULL | | |
| `PhotoStoredFileId` | uniqueidentifier | YES | NULL | FK -> StoredFiles.Id ON DELETE SET NULL | |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `CreatedById` | uniqueidentifier | NO | | FK -> AspNetUsers.Id | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedById` | uniqueidentifier | NO | | FK -> AspNetUsers.Id | |
| `Version` | rowversion | NO | auto | | Concurrency token |

- **IX_Livestock_Company_Type_Status_IsDeleted_Alive**: filtered index on `WHERE Status=0 AND IsDeleted=0` (stock-on-hand queries).
- **IX_Livestock_FarmId**: for per-farm listings.
- **IX_Livestock_SoldSaleId**: foreign key index.

### `WeightEntries` (immutable — no UPDATE allowed, trigger/CHECK enforces)

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK (clustered) | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `LivestockId` | uniqueidentifier | NO | | FK -> Livestock.Id ON DELETE CASCADE, IX (LivestockId + WeighedAt DESC) | |
| `WeighedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `WeightKg` | decimal(18,4) | NO | | CHECK >0 | Canonical |
| `WeightUnit` | char(2) | NO | | CHECK IN ('kg','lb') | Original entered unit |
| `WeighedById` | uniqueidentifier | NO | | FK -> AspNetUsers.Id | |
| `Notes` | nvarchar(500) | YES | NULL | | E.g. reason for correction entry |
| `IsDeleted` | bit | NO | 0 | | Soft delete = correction reversal |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |

- Database trigger: `AFTER INSERT, UPDATE, DELETE ON WeightEntries` refreshes `Livestock.CurrentWeightKg` and `CurrentWeightAsAt` for the referenced LivestockId (using the most recent non-deleted entry).

---

## 4. Customers & Suppliers

### `Customers`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `Code` | nvarchar(32) | NO | | UK (CompanyId + Code + !IsDeleted) | Human friendly |
| `Name` | nvarchar(200) | NO | | IX | |
| `LegalName` | nvarchar(250) | YES | NULL | | |
| `TaxId` | nvarchar(50) | YES | NULL | | VAT/CIF |
| `BillingAddress_*` | 6 cols matching Companies.LegalAddress | NO/YES | | Owned entity (BillingAddress) | |
| `ShippingAddress_*` | same 6 cols | YES | NULL | Owned entity (ShippingAddress), nullable | |
| `Email` | nvarchar(254) | YES | NULL | | |
| `Phone` | nvarchar(30) | YES | NULL | | |
| `PaymentTermsDays` | int | YES | NULL | CHECK BETWEEN 0 AND 180 | NULL = inherit Company default (30) |
| `CreditLimit` | decimal(18,2) | YES | NULL | CHECK >=0 | NULL = unlimited |
| `CurrencyCode` | char(3) | NO | N'USD' | CHECK IN ('USD','EUR') | Inherited from Company, not user-editable in v1 |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `IsActive` | bit | NO | 1 | | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `Version` | rowversion | NO | auto | | |

### `Suppliers`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `Code` | nvarchar(32) | NO | | UK (CompanyId + Code + !IsDeleted) | |
| `Name` | nvarchar(200) | NO | | IX | |
| `LegalName` | nvarchar(250) | YES | NULL | | |
| `TaxId` | nvarchar(50) | YES | NULL | | |
| `Address_*` | 6 cols | NO/YES | | Owned entity | |
| `Email` | nvarchar(254) | YES | NULL | | |
| `Phone` | nvarchar(30) | YES | NULL | | |
| `BankAccountName` | nvarchar(128) | YES | NULL | | |
| `BankAccountIban` | nvarchar(34) | YES | NULL | | |
| `BankAccountSwift` | nvarchar(11) | YES | NULL | | |
| `PaymentTermsDays` | int | YES | NULL | CHECK BETWEEN 0 AND 180 | |
| `CurrencyCode` | char(3) | NO | N'USD' | CHECK IN ('USD','EUR') | |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `IsActive` | bit | NO | 1 | | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `Version` | rowversion | NO | auto | | |

---

## 5. Purchases & Expenses

### `DocumentSequences` (single shared table for all per-company counters)

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, UK (CompanyId + DocumentType + Year) | |
| `DocumentType` | char(3) | NO | | CHECK IN ('PUR','SOR','INV','PAY','REC','EXP') | |
| `Year` | smallint | NO | | E.g. 2026 | |
| `NextValue` | int | NO | 1 | CHECK >=1 | Atomically incremented via `UPDATE … OUTPUT inserted.NextValue` |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |

### `Purchases`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `PurchaseNumber` | nvarchar(16) | NO | | UK (CompanyId + PurchaseNumber + !IsDeleted) | e.g. 2026-00001 |
| `SupplierId` | uniqueidentifier | NO | | FK | |
| `FarmId` | uniqueidentifier | NO | | FK (destination farm) | |
| `PurchaseDate` | date | NO | | IX (CompanyId + PurchaseDate DESC) | |
| `SupplierInvoiceNo` | nvarchar(64) | YES | NULL | | |
| `Status` | tinyint | NO | 0 | CHECK IN (0,1,2) | 0=Draft, 1=Posted, 2=Voided |
| `PostedAt` | datetimeoffset(7) | YES | NULL | | |
| `PostedById` | uniqueidentifier | YES | NULL | FK -> AspNetUsers | |
| `VoidedAt` | datetimeoffset(7) | YES | NULL | | |
| `VoidedById` | uniqueidentifier | YES | NULL | FK -> AspNetUsers | |
| `VoidReason` | nvarchar(500) | YES | NULL | | |
| `DiscountPct` | decimal(5,2) | NO | 0.00 | CHECK BETWEEN 0 AND 100 | Header-level default |
| `TaxRate` | decimal(5,2) | NO | 20.00 | CHECK BETWEEN 0 AND 35 | |
| `PricesIncludeTax` | bit | NO | 0 | | |
| `SubTotal` | decimal(18,2) | NO | 0.00 | CHECK >=0 | Sum of line totals pre-tax |
| `TotalDiscount` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |
| `TaxableAmount` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |
| `TotalTax` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |
| `GrandTotal` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |
| `CurrencyCode` | char(3) | NO | N'USD' | CHECK IN ('USD','EUR') | Snapshot |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `CreatedById` | uniqueidentifier | NO | | FK -> AspNetUsers | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedById` | uniqueidentifier | NO | | FK -> AspNetUsers | |
| `Version` | rowversion | NO | auto | | |

### `PurchaseLines`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK | |
| `PurchaseId` | uniqueidentifier | NO | | FK -> Purchases.Id ON DELETE CASCADE, IX | |
| `LineNo` | smallint | NO | | CHECK >0, UK(PurchaseId+LineNo) | |
| `Description` | nvarchar(256) | YES | NULL | | Override description |
| `LivestockType` | char(2) | NO | | CHECK IN ('Ah','Su','Sa','Ad','Sd') | |
| `Quantity` | int | NO | | CHECK >0 | Head count |
| `UnitWeightKg` | decimal(18,4) | NO | | CHECK >0 | |
| `UnitWeightUnit` | char(2) | NO | N'kg' | CHECK IN ('kg','lb') | Original unit |
| `UnitPrice` | decimal(18,2) | NO | | CHECK >=0 | Per head |
| `DiscountPct` | decimal(5,2) | NO | 0.00 | CHECK BETWEEN 0 AND 100 | |
| `TaxRate` | decimal(5,2) | NO | 20.00 | CHECK BETWEEN 0 AND 35 | |
| `LineTotal` | decimal(18,2) | NO | 0.00 | CHECK >=0 | qty × unitprice × (1-disc) |
| `TaxableAmount` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |
| `TaxAmount` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |
| `GrandTotalLine` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |

### `Expenses`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `ExpenseNumber` | nvarchar(16) | NO | | UK | |
| `ExpenseDate` | date | NO | | IX | |
| `Category` | nvarchar(32) | NO | | CHECK IN ('Feed','Vet','Transport','Utilities','Labor','Other') + extensible lookup |
| `SupplierId` | uniqueidentifier | YES | NULL | FK ON DELETE SET NULL | |
| `FarmId` | uniqueidentifier | YES | NULL | FK ON DELETE SET NULL | |
| `Description` | nvarchar(500) | YES | NULL | | |
| `Amount` | decimal(18,2) | NO | | CHECK >0 | Net amount |
| `TaxRate` | decimal(5,2) | NO | 20.00 | | |
| `TaxAmount` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |
| `GrandTotal` | decimal(18,2) | NO | 0.00 | CHECK >=0 | |
| `PaymentMethod` | nvarchar(32) | NO | | Cash/BankTransfer/Check/Card |
| `ReferenceNo` | nvarchar(64) | YES | NULL | | Check/transaction |
| `ReceiptStoredFileId` | uniqueidentifier | YES | NULL | FK -> StoredFiles ON DELETE SET NULL | |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `CreatedById` | uniqueidentifier | NO | | FK | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedById` | uniqueidentifier | NO | | FK | |
| `Version` | rowversion | NO | auto | | |

---

## 6. Sales & Invoicing

### `SalesOrders`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `SalesOrderNumber` | nvarchar(16) | NO | | UK | |
| `CustomerId` | uniqueidentifier | NO | | FK | |
| `FarmId` | uniqueidentifier | NO | | FK | |
| `OrderDate` | date | NO | | IX | |
| `RequestedDeliveryDate` | date | YES | NULL | | |
| `Status` | tinyint | NO | 0 | CHECK IN (0,1,2,3) | 0=Draft, 1=Confirmed, 2=Cancelled, 3=Invoiced |
| `InvoicedAt` | datetimeoffset(7) | YES | NULL | | |
| `DiscountPct` | decimal(5,2) | NO | 0.00 | | |
| `TaxRate` | decimal(5,2) | NO | 20.00 | | |
| `PricesIncludeTax` | bit | NO | 0 | | |
| `SubTotal` | decimal(18,2) | NO | 0.00 | | |
| `TotalDiscount` | decimal(18,2) | NO | 0.00 | | |
| `TaxableAmount` | decimal(18,2) | NO | 0.00 | | |
| `TotalTax` | decimal(18,2) | NO | 0.00 | | |
| `GrandTotal` | decimal(18,2) | NO | 0.00 | | |
| `CurrencyCode` | char(3) | NO | N'USD' | | |
| `CustomerPO` | nvarchar(64) | YES | NULL | | Customer Purchase Order ref |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `CreatedById` | uniqueidentifier | NO | | FK | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedById` | uniqueidentifier | NO | | FK | |
| `Version` | rowversion | NO | auto | | |

### `SalesOrderLines`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK | |
| `SalesOrderId` | uniqueidentifier | NO | | FK ON DELETE CASCADE, IX | |
| `LineNo` | smallint | NO | | UK(SalesOrderId+LineNo) | |
| `IsSpecificLivestock` | bit | NO | 0 | | 1 = references specific Livestock by TagId |
| `LivestockId` | uniqueidentifier | YES | NULL | FK -> Livestock ON DELETE SET NULL | If IsSpecificLivestock=1 |
| `LivestockType` | char(2) | NO | | CHECK IN (Ah,Su,Sa,Ad,Sd) | |
| `Description` | nvarchar(256) | YES | NULL | | |
| `Quantity` | int | NO | | CHECK >0 | |
| `UnitWeightKg` | decimal(18,4) | YES | NULL | CHECK >=0 | Snapshot per head |
| `UnitWeightUnit` | char(2) | YES | N'kg' | | |
| `UnitPrice` | decimal(18,2) | NO | | CHECK >=0 | Per head (or per kg if pricing unit = kg) |
| `DiscountPct` | decimal(5,2) | NO | 0.00 | | |
| `TaxRate` | decimal(5,2) | NO | 20.00 | | |
| `LineTotal` | decimal(18,2) | NO | 0.00 | | |
| `TaxableAmount` | decimal(18,2) | NO | 0.00 | | |
| `TaxAmount` | decimal(18,2) | NO | 0.00 | | |
| `GrandTotalLine` | decimal(18,2) | NO | 0.00 | | |

### `Invoices`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `InvoiceNumber` | nvarchar(16) | NO | | UK (CompanyId + Number) | |
| `SalesOrderId` | uniqueidentifier | YES | NULL | FK ON DELETE SET NULL | Source SO or NULL for direct |
| `CustomerId` | uniqueidentifier | NO | | FK, IX | |
| `FarmId` | uniqueidentifier | NO | | FK | |
| `InvoiceDate` | date | NO | | IX (CompanyId + InvoiceDate DESC) | |
| `DueDate` | date | NO | | IX (CompanyId + DueDate) | For overdue/aging queries |
| `Status` | tinyint | NO | 0 | CHECK IN (0,1,2,3) | 0=Draft, 1=Sent, 2=Paid, 3=Voided |
| `SentAt` | datetimeoffset(7) | YES | NULL | | |
| `SentCount` | int | NO | 0 | CHECK >=0 | |
| `LastSentStatus` | nvarchar(32) | YES | NULL | | Queued/Sent/Failed |
| `LastSentErrorMessage` | nvarchar(1000) | YES | NULL | | |
| `PaidAt` | datetimeoffset(7) | YES | NULL | | |
| `VoidedAt` | datetimeoffset(7) | YES | NULL | | |
| `VoidedById` | uniqueidentifier | YES | NULL | FK | |
| `VoidReason` | nvarchar(500) | YES | NULL | | |
| `BillingSnapshot_Name` | nvarchar(250) | NO | | Snapshot cols = owned entity | |
| `BillingSnapshot_Address_*` | 6 cols | NO | | Billing snapshot frozen at invoice time | |
| `BillingSnapshot_TaxId` | nvarchar(50) | YES | NULL | | |
| `BillingSnapshot_Email` | nvarchar(254) | YES | NULL | | |
| `ShippingSnapshot_*` | analogous cols | YES | NULL | | |
| `DiscountPct` | decimal(5,2) | NO | 0.00 | | |
| `TaxRate` | decimal(5,2) | NO | 20.00 | | |
| `PricesIncludeTax` | bit | NO | 0 | | |
| `SubTotal` | decimal(18,2) | NO | 0.00 | | |
| `TotalDiscount` | decimal(18,2) | NO | 0.00 | | |
| `TaxableAmount` | decimal(18,2) | NO | 0.00 | | |
| `TotalTax` | decimal(18,2) | NO | 0.00 | | |
| `GrandTotal` | decimal(18,2) | NO | 0.00 | | |
| `CurrencyCode` | char(3) | NO | N'USD' | | |
| `AmountPaid` | decimal(18,2) | NO | 0.00 | CHECK >=0 | Denormalized sum of PaymentAllocations |
| `BalanceDue` | decimal(18,2) | NO | 0.00 | | Computed col or maintained by app |
| `CustomerPO` | nvarchar(64) | YES | NULL | | |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `ArchivedPdfStoredFileId` | uniqueidentifier | YES | NULL | FK -> StoredFiles ON DELETE SET NULL | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `CreatedById` | uniqueidentifier | NO | | FK | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedById` | uniqueidentifier | NO | | FK | |
| `Version` | rowversion | NO | auto | | |

- **IX_Invoices_Company_DueDate_BalanceDue**: filtered `WHERE Status IN (0,1) AND BalanceDue > 0` (overdue aging queries).

### `InvoiceLines` (same structure as SalesOrderLines plus FK to Invoices)

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK | |
| `InvoiceId` | uniqueidentifier | NO | | FK ON DELETE CASCADE, IX | |
| `SalesOrderLineId` | bigint | YES | NULL | FK -> SalesOrderLines ON DELETE SET NULL | Traceability to SO line |
| `LineNo` | smallint | NO | | UK(InvoiceId+LineNo) | |
| `Description` | nvarchar(256) | YES | NULL | | |
| `LivestockId` | uniqueidentifier | YES | NULL | FK -> Livestock ON DELETE SET NULL | If line tied to specific head |
| `LivestockType` | char(2) | NO | | | |
| `Quantity` | int | NO | | CHECK >0 | |
| `UnitWeightKg` | decimal(18,4) | YES | NULL | | Frozen snapshot |
| `UnitWeightUnit` | char(2) | YES | N'kg' | | Snapshot |
| `UnitPrice` | decimal(18,2) | NO | | | Snapshot |
| `DiscountPct` | decimal(5,2) | NO | 0.00 | | |
| `TaxRate` | decimal(5,2) | NO | 20.00 | | |
| `LineTotal` | decimal(18,2) | NO | 0.00 | | |
| `TaxableAmount` | decimal(18,2) | NO | 0.00 | | |
| `TaxAmount` | decimal(18,2) | NO | 0.00 | | |
| `GrandTotalLine` | decimal(18,2) | NO | 0.00 | | |

---

## 7. Payments & Receipts

### `Payments` (Incoming from Customers)

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `PaymentNumber` | nvarchar(16) | NO | | UK | |
| `CustomerId` | uniqueidentifier | NO | | FK, IX | |
| `PaymentDate` | date | NO | | IX | |
| `PaymentMethod` | nvarchar(32) | NO | | |
| `ReferenceNo` | nvarchar(64) | YES | NULL | | Check or transaction |
| `Amount` | decimal(18,2) | NO | | CHECK >0 | |
| `CurrencyCode` | char(3) | NO | N'USD' | | |
| `DepositedAccount` | nvarchar(64) | YES | NULL | | Bank account ref |
| `TotalAllocated` | decimal(18,2) | NO | 0.00 | CHECK >=0 | Sum of allocations |
| `UnallocatedAmount` | decimal(18,2) | NO | 0.00 | CHECK >=0 | Amount - TotalAllocated |
| `IsVoided` | bit | NO | 0 | | |
| `VoidedAt` | datetimeoffset(7) | YES | NULL | | |
| `VoidedById` | uniqueidentifier | YES | NULL | FK | |
| `VoidReason` | nvarchar(500) | YES | NULL | | |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| `IsDeleted` | bit | NO | 0 | | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `CreatedById` | uniqueidentifier | NO | | FK | |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedById` | uniqueidentifier | NO | | FK | |
| `Version` | rowversion | NO | auto | | |

### `PaymentAllocations`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK | |
| `PaymentId` | uniqueidentifier | NO | | FK ON DELETE CASCADE, IX | |
| `InvoiceId` | uniqueidentifier | NO | | FK -> Invoices, IX | |
| `Amount` | decimal(18,2) | NO | | CHECK >0 | |
| `AllocatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `IsReversed` | bit | NO | 0 | | 1 if parent payment voided |
| `ReversedAt` | datetimeoffset(7) | YES | NULL | | |

### `Receipts` (Outgoing to Suppliers)

Mirrors `Payments` with SupplierId instead of CustomerId, plus analogous allocations to `Purchases`:

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | |
| `ReceiptNumber` | nvarchar(16) | NO | | UK | |
| `SupplierId` | uniqueidentifier | NO | | FK, IX | |
| `PaymentDate` | date | NO | | |
| `PaymentMethod` | nvarchar(32) | NO | | |
| `ReferenceNo` | nvarchar(64) | YES | NULL | |
| `Amount` | decimal(18,2) | NO | | CHECK >0 | |
| `CurrencyCode` | char(3) | NO | N'USD' | | |
| `BankAccountOut` | nvarchar(64) | YES | NULL | | |
| `TotalAllocated` | decimal(18,2) | NO | 0.00 | | |
| `UnallocatedAmount` | decimal(18,2) | NO | 0.00 | | |
| `IsVoided` / void cols | … | … | … | |
| `Notes` | nvarchar(1000) | YES | NULL | | |
| audit/version cols | … | … | … | | |

### `ReceiptAllocations`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK | |
| `ReceiptId` | uniqueidentifier | NO | | FK ON DELETE CASCADE, IX | |
| `PurchaseId` | uniqueidentifier | NO | | FK -> Purchases, IX | |
| `Amount` | decimal(18,2) | NO | | CHECK >0 | |
| `AllocatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `IsReversed` | bit | NO | 0 | | |

---

## 8. Shared / Cross-Module

### `StoredFiles` (file registry for attachments, PDFs, logos, photos)

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | uniqueidentifier | NO | newsequentialid() | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, IX | NULL disallowed; sysadmin uploads use a "system" company row |
| `StorageProvider` | char(3) | NO | N'LOC' | CHECK IN ('LOC','AZB','S3') | Local / Azure Blob / S3 |
| `StorageKey` | nvarchar(512) | NO | | UK (CompanyId + StorageKey) | Path / blob URL |
| `OriginalFileName` | nvarchar(256) | NO | | User-uploaded name |
| `FileExtension` | varchar(10) | NO | | Include dot, e.g. `.pdf` |
| `SizeBytes` | bigint | NO | | CHECK >0 |
| `MimeType` | varchar(128) | NO | | Detected server-side; not trusted from client |
| `HashSHA256` | binary(32) | NO | | IX; dedupe anchor |
| `Category` | nvarchar(32) | NO | | InvoicePdf / LivestockPhoto / CompanyLogo / PurchaseDoc / ExpenseReceipt / Other |
| `CreatedById` | uniqueidentifier | NO | | FK -> AspNetUsers | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | IX | |
| `IsDeleted` | bit | NO | 0 | | Soft delete → actual deletion delayed by cleanup job |

### `AuditLogs` (structured audit log for every mutation)

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK (clustered, partition by CreatedAt monthly optional) |
| `CompanyId` | uniqueidentifier | YES | NULL | IX | NULL for system-wide actions |
| `UserId` | uniqueidentifier | YES | NULL | IX | NULL for anonymous/system |
| `UserDisplayName` | nvarchar(128) | YES | NULL | Denormalized, tolerates user soft-delete |
| `EntityType` | nvarchar(128) | NO | | Full CLR type name or short code |
| `EntityId` | nvarchar(64) | NO | | Stringified PK (guid or bigint); allows generic indexing |
| `Operation` | char(1) | NO | | CHECK IN ('C','U','D','V') | Create/Update/Delete/Void |
| `OldValues` | nvarchar(max) | YES | NULL | JSON property bag; NULL on Create |
| `NewValues` | nvarchar(max) | YES | NULL | JSON; NULL on Delete |
| `ChangedColumns` | nvarchar(max) | YES | NULL | JSON array of changed column names |
| `IpAddress` | varchar(45) | YES | NULL | IPv4 or IPv6 |
| `UserAgent` | nvarchar(512) | YES | NULL | |
| `CorrelationId` | uniqueidentifier | YES | NULL | Matches Serilog request trace id |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | IX (clustered optionally) | |

### `OutboundEmails`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK | |
| `CompanyId` | uniqueidentifier | YES | NULL | IX | NULL for identity/pw-reset emails |
| `TemplateCode` | nvarchar(64) | YES | NULL | InvoiceSent / PaymentReceipt / PasswordReset / InviteUser / Custom |
| `ToAddress` | nvarchar(254) | NO | | |
| `CCAddresses` | nvarchar(1024) | YES | NULL | Semicolon separated |
| `Subject` | nvarchar(256) | NO | | |
| `BodyPreview` | nvarchar(500) | YES | NULL | First ~300 chars stripped HTML |
| `Status` | tinyint | NO | 0 | CHECK IN (0,1,2,3) | 0=Queued, 1=Sent, 2=Failed, 3=DeadLetter |
| `Attempts` | smallint | NO | 0 | CHECK >=0 | |
| `LastAttemptAt` | datetimeoffset(7) | YES | NULL | | |
| `LastError` | nvarchar(2000) | YES | NULL | |
| `SentAt` | datetimeoffset(7) | YES | NULL | IX | |
| `SendProvider` | nvarchar(32) | YES | NULL | SendGrid/SMTP |
| `ProviderMessageId` | nvarchar(128) | YES | NULL | |
| `RelatedEntityType` | nvarchar(64) | YES | NULL | e.g. Invoice |
| `RelatedEntityId` | nvarchar(64) | YES | NULL | |
| `CreatedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `JobId` | nvarchar(64) | YES | NULL | Hangfire job id |

### `OutboundEmailAttachments`

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | bigint | NO | IDENTITY(1,1) | PK | |
| `OutboundEmailId` | bigint | NO | | FK ON DELETE CASCADE | |
| `StoredFileId` | uniqueidentifier | NO | | FK -> StoredFiles | |
| `AttachmentName` | nvarchar(256) | NO | | Override filename as sent |

### `AppSettings` (per-company runtime toggle overrides)

| Column | Type | Null | Default | Key/Index | Notes |
|---|---|---|---|---|---|
| `Id` | int | NO | IDENTITY(1,1) | PK | |
| `CompanyId` | uniqueidentifier | NO | | FK, UK(CompanyId+Key) | |
| `Key` | nvarchar(128) | NO | | |
| `Value` | nvarchar(max) | NO | | JSON scalar |
| `ModifiedAt` | datetimeoffset(7) | NO | sysdatetimeoffset() | | |
| `ModifiedById` | uniqueidentifier | NO | | FK | |

---

## Summary: Table Inventory

| # | Table | Primary Key | Company-scoped | Soft Delete | Concurrency Token |
|---|---|---|---|---|---|
| 1 | Companies | Id (guid) | n/a (root) | YES | rowversion |
| 2 | Farms | Id (guid) | YES (IX) | YES | rowversion |
| 3 | AspNetUsers (extended) | Id (guid) | YES (nullable) | YES | rowversion |
| 4 | AspNetRoles (extended) | Id (guid) | NO | NO | — |
| 5 | UserRoleAssignmentAudits | Id (bigint) | via UserId | NO | — |
| 6 | Livestock | Id (guid) | YES (IX) | YES | rowversion |
| 7 | WeightEntries | Id (bigint) | YES (IX) | YES (corrections) | — |
| 8 | Customers | Id (guid) | YES (IX) | YES | rowversion |
| 9 | Suppliers | Id (guid) | YES (IX) | YES | rowversion |
| 10 | DocumentSequences | Id (bigint) | YES (UK) | NO | — |
| 11 | Purchases | Id (guid) | YES (IX) | YES | rowversion |
| 12 | PurchaseLines | Id (bigint) | via PurchaseId | NO | — |
| 13 | Expenses | Id (guid) | YES (IX) | YES | rowversion |
| 14 | SalesOrders | Id (guid) | YES (IX) | YES | rowversion |
| 15 | SalesOrderLines | Id (bigint) | via SalesOrderId | NO | — |
| 16 | Invoices | Id (guid) | YES (IX) | YES | rowversion |
| 17 | InvoiceLines | Id (bigint) | via InvoiceId | NO | — |
| 18 | Payments | Id (guid) | YES (IX) | YES | rowversion |
| 19 | PaymentAllocations | Id (bigint) | via PaymentId | NO | — |
| 20 | Receipts | Id (guid) | YES (IX) | YES | rowversion |
| 21 | ReceiptAllocations | Id (bigint) | via ReceiptId | NO | — |
| 22 | StoredFiles | Id (guid) | YES (IX) | YES | — |
| 23 | AuditLogs | Id (bigint) | YES (IX) | NO | — |
| 24 | OutboundEmails | Id (bigint) | YES (IX) | NO | — |
| 25 | OutboundEmailAttachments | Id (bigint) | via EmailId | NO | — |
| 26 | AppSettings | Id (int) | YES (UK) | NO | — |
| H | Hangfire tables (set) | — | NO | — | — |

- **Identity standard tables**: AspNetUserRoles, AspNetUserClaims, AspNetRoleClaims, AspNetUserLogins, AspNetUserTokens (not re-documented; default schema).
- **Total**: 26 app-defined tables + 5 Identity standard + Hangfire (~12 Hangfire tables) ≈ 43 total.

---

## Global Query Filters (EF Core)

- All entities implementing `IHaveCompanyId`: `e.CompanyId == _currentCompanyContext.CompanyId || _currentCompanyContext.IsSystemAdministratorImpersonatingCompany == false` (infrastructure concern).
- All entities implementing `ISoftDeletable`: `!e.IsDeleted`.
- Filters applied only when `DbContext` is not configured as `DisableGlobalFilters = true` (used by SystemAdministrator audit reports and the seeding migration).
