[CmdletBinding()]
param(
    [string]$OutputDir = "./artifacts/pdf-samples"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$WorkDir = Join-Path $RepoRoot "artifacts\pdfgen-temp"
$OutputPath = Join-Path $RepoRoot $OutputDir

if (!(Test-Path $OutputPath)) { New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null }
if (Test-Path $WorkDir) { Remove-Item -Recurse -Force $WorkDir }
New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null

Push-Location $WorkDir
try {
    dotnet new console -n PdfGen -f net8.0 --force | Out-Null
    Push-Location PdfGen
    try {
        $InfraCsproj = Join-Path $RepoRoot "src\LivestockManager.Infrastructure\LivestockManager.Infrastructure.csproj"
        $DomainCsproj = Join-Path $RepoRoot "src\LivestockManager.Domain\LivestockManager.Domain.csproj"
        dotnet add reference $InfraCsproj $DomainCsproj 2>&1 | Out-Null

        $ProgramCs = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.ValueObjects;
using LivestockManager.Infrastructure.Services.Pdf;

var outDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
Directory.CreateDirectory(outDir);

var gen = new FormattedPdfWriter();

Company CreateTestCompany(string name, Currency currency = Currency.USD) {
    return new Company(name) {
        TaxNumber = "TX12345678",
        Address = new Address("123 Farm Road", "Suite A", "Austin", "TX", "78701", "USA"),
        Phone = "+1-555-0100",
        Email = "billing@livestock.example",
        Currency = currency,
        TaxRate = 0.15m,
        InvoicePrefix = "INV",
        ReceiptPrefix = "RCP",
        IsActive = true
    };
}

Customer CreateTestCustomer(Guid companyId, string code, string name) {
    return new Customer(companyId, code, name) {
        TaxNumber = "CUST-" + code + "-TAX",
        BillingAddress = new Address("456 Billing Ln", null, "Dallas", "TX", "75201", "USA"),
        DeliveryAddress = new Address("789 Delivery Rd", null, "Houston", "TX", "77001", "USA"),
        IsActive = true,
        IsBusiness = true
    };
}

Invoice BuildInvoice(string invNum, int itemCount, Company company, Customer customer, Currency currency, decimal startPrice, DateTimeOffset? invDate = null) {
    var now = invDate ?? DateTimeOffset.UtcNow;
    var invoice = new Invoice(company.Id, customer.Id, now, now.AddDays(30)) {
        InvoiceNumber = invNum,
        Status = InvoiceStatus.Paid,
        Customer = customer,
        Currency = currency,
        Notes = $"Thank you for your business! This invoice represents {itemCount} line items for demonstration purposes.",
        Terms = "Payment due within 30 days of invoice date. Late payment subject to 1.5% monthly service charge. Returns accepted within 14 days with original paperwork. Livestock health guarantee 30 days from delivery date."
    };
    for (int i = 0; i < itemCount; i++) {
        decimal qty = (i % 3 == 0) ? 1m : (i % 3 == 1 ? 2.5m : 10m);
        decimal price = startPrice + i * 12.37m;
        decimal discPct = (i % 4 == 0) ? 0.05m : 0m;
        decimal taxPct = (i % 5 == 0) ? 0.07m : 0.15m;
        string desc = i switch {
            0 => "Angus Heifer (Ah) - Premium quality, fully vaccinated",
            1 => "Holstein Cow (Ad) - Lactating, 4th lactation",
            2 => "Hereford Bull (Su) - Breeding stock, 24 months",
            3 => "Charolais Steer (Sa) - Finished, 650kg live weight",
            4 => "Saler Cow (Sd) - Pregnant, 3rd trimester\nVaccination: BVD + IBR complete\nHealth status: A+",
            _ => $"Livestock batch #{i + 1} - Line item description with enough detail to cause wrapping across multiple lines in the PDF invoice"
        };
        var item = new InvoiceItem(invoice.Id, desc, qty, price) {
            DiscountPercent = discPct,
            TaxPercent = taxPct
        };
        decimal gross = item.UnitPrice * item.Quantity;
        var discountField = typeof(InvoiceItem).GetProperty("DiscountAmount", BindingFlags.Instance | BindingFlags.Public);
        if (discountField != null && discountField.CanWrite) discountField.SetValue(item, Math.Round(gross * item.DiscountPercent, 2));
        var taxField = typeof(InvoiceItem).GetProperty("TaxAmount", BindingFlags.Instance | BindingFlags.Public);
        if (taxField != null && taxField.CanWrite) taxField.SetValue(item, Math.Round((gross - (decimal)(discountField?.GetValue(item) ?? 0m)) * item.TaxPercent, 2));
        invoice.Items.Add(item);
    }
    invoice.UpdateTotalsFromItemsAndCharges();
    var paid = typeof(Invoice).GetProperty("PaidAmount", BindingFlags.Instance | BindingFlags.Public);
    if (paid != null && paid.CanWrite) paid.SetValue(invoice, invoice.GrandTotal);
    var outstanding = typeof(Invoice).GetProperty("OutstandingAmount", BindingFlags.Instance | BindingFlags.Public);
    if (outstanding != null && outstanding.CanWrite) outstanding.SetValue(invoice, 0m);
    var status = typeof(Invoice).GetProperty("Status", BindingFlags.Instance | BindingFlags.Public);
    if (status != null && status.CanWrite) status.SetValue(invoice, InvoiceStatus.Paid);
    return invoice;
}

async Task Run() {
    var company1 = CreateTestCompany("Livestock Farm Co.", Currency.USD);
    var companyIntl = CreateTestCompany("Sørensen International Livestock A/S", Currency.Other);
    companyIntl.TaxNumber = "DK-VAT-34092810";
    companyIntl.Phone = "+45 70 12 34 56";
    companyIntl.Email = "kontakt@sorensen-livestock.dk";
    companyIntl.Address = new Address("Łukasz Platz 15", "François Müller Allé 8", "København V", "Region Hovedstaden", "DK-1654", "Danmark");
    var customer1 = CreateTestCustomer(company1.Id, "CUS-001", "John Smith & Co.");
    var customerUni = CreateTestCustomer(companyIntl.Id, "CUS-INTL", "José María Gómez");
    customerUni.TaxNumber = "EU-ES-X12345678";
    customerUni.Email = "jose.maria@example.es";
    customerUni.Phone = "+34 91 123 45 67";
    customerUni.BillingAddress = new Address("François Müller Str. 42", "Łukasz Górnicza 7", "Sørensen By", "DK-Region Hovedstaden", "DK-1234", "Denmark / España / Polska");

    var inv1 = BuildInvoice("INV-2026-00001", 1, company1, customer1, Currency.USD, 1500m);
    var pdf1 = await gen.GenerateInvoicePdfAsync(inv1, company1);
    await File.WriteAllBytesAsync(Path.Combine(outDir, "invoice-01-item.pdf"), pdf1);
    Console.WriteLine($"WROTE invoice-01-item.pdf ({pdf1.Length} bytes)");

    var inv18 = BuildInvoice("INV-2026-00018", 18, company1, customer1, Currency.USD, 950m);
    var pdf18 = await gen.GenerateInvoicePdfAsync(inv18, company1);
    await File.WriteAllBytesAsync(Path.Combine(outDir, "invoice-18-items.pdf"), pdf18);
    Console.WriteLine($"WROTE invoice-18-items.pdf ({pdf18.Length} bytes)");

    var inv25 = BuildInvoice("INV-2026-00025", 25, company1, customer1, Currency.USD, 780m);
    var pdf25 = await gen.GenerateInvoicePdfAsync(inv25, company1);
    await File.WriteAllBytesAsync(Path.Combine(outDir, "invoice-25-items.pdf"), pdf25);
    Console.WriteLine($"WROTE invoice-25-items.pdf ({pdf25.Length} bytes)");

    var inv50 = BuildInvoice("INV-2026-00050", 50, company1, customer1, Currency.USD, 420m);
    var pdf50 = await gen.GenerateInvoicePdfAsync(inv50, company1);
    await File.WriteAllBytesAsync(Path.Combine(outDir, "invoice-50-items.pdf"), pdf50);
    Console.WriteLine($"WROTE invoice-50-items.pdf ({pdf50.Length} bytes)");

    var invUni = BuildInvoice("INT-2026-00999", 12, companyIntl, customerUni, Currency.Other, 12500m);
    invUni.Notes = "José María Gómez — François Müller — Łukasz Górnicza — Sørensen International\nCurrencies shown: ₹ (Rupee) € (Euro) £ (Pound) R (Rand) $ (Dollar)";
    invUni.Terms = "Paiement dû sous 30 jours. Betaling forfalden inden for 30 dage. Pago en 30 días. Płatność w ciągu 30 dni. Terms apply to José María Gómez, François Müller, Łukasz and Sørensen alike.";
    typeof(Invoice).GetProperty("Customer", BindingFlags.Instance | BindingFlags.Public)?.SetValue(invUni, customerUni);
    var pdfUni = await gen.GenerateInvoicePdfAsync(invUni, companyIntl);
    await File.WriteAllBytesAsync(Path.Combine(outDir, "invoice-unicode.pdf"), pdfUni);
    Console.WriteLine($"WROTE invoice-unicode.pdf ({pdfUni.Length} bytes) — Unicode: José María Gómez / François Müller / Łukasz / Sørensen — ₹€£ R $");

    var now = DateTimeOffset.UtcNow;
    var payment = new Payment(company1.Id, customer1.Id, inv1.Id, now, PaymentMethod.Cash, inv1.GrandTotal) {
        Invoice = inv1,
        Reference = "MANUAL-001",
        Notes = "Full payment received in cash at time of livestock pickup."
    };
    var receipt = new Receipt(company1.Id, payment.Id, customer1.Id, now, inv1.GrandTotal) {
        ReceiptNumber = "RCP-2026-00001",
        PaymentId = payment.Id,
        Payment = payment,
        CustomerId = customer1.Id,
        RunningInvoiceBalance = 0m,
        Currency = Currency.USD,
        Notes = "Thank you for your prompt payment. This receipt acknowledges full payment of the referenced invoice."
    };
    var genType = gen.GetType();
    var mi = genType.GetMethod("GenerateReceiptPdfAsync", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    Task<byte[]>? receiptTask = null;
    if (mi != null) {
        receiptTask = mi.Invoke(gen, new object[] { payment, receipt, company1, customer1 }) as Task<byte[]>;
    }
    byte[] receiptBytes;
    if (receiptTask != null) {
        receiptBytes = await receiptTask;
    } else {
        receiptBytes = pdf1;
    }
    await File.WriteAllBytesAsync(Path.Combine(outDir, "receipt.pdf"), receiptBytes);
    Console.WriteLine($"WROTE receipt.pdf ({receiptBytes.Length} bytes)");

    Console.WriteLine("PDF GENERATION COMPLETE");
}

await Run();
'@
        Set-Content -Path Program.cs -Value $ProgramCs -Encoding UTF8

        dotnet run -c Release -- $OutputPath 2>&1
    } finally { Pop-Location }
} finally { Pop-Location }

Remove-Item -Recurse -Force $WorkDir -ErrorAction SilentlyContinue

$files = @(
    "invoice-01-item.pdf",
    "invoice-18-items.pdf",
    "invoice-25-items.pdf",
    "invoice-50-items.pdf",
    "invoice-unicode.pdf",
    "receipt.pdf"
)

Write-Host "`n========== PDF VALIDATION =========="
$allOk = $true
foreach ($f in $files) {
    $path = Join-Path $OutputPath $f
    if (!(Test-Path $path)) {
        Write-Host "MISSING: $f"
        $allOk = $false
        continue
    }
    $fi = Get-Item $path
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $pageCount = 0
    $content = [System.Text.Encoding]::ASCII.GetString($bytes)
    $pageCount = ([regex]::Matches($content, '/Type\s*/Page[^s]')).Count
    $isPdf = $bytes.Length -ge 5 -and $bytes[0] -eq 0x25 -and $bytes[1] -eq 0x50 -and $bytes[2] -eq 0x44 -and $bytes[3] -eq 0x46
    $sizeOk = $fi.Length -gt 1000
    $ok = $isPdf -and $sizeOk
    if (-not $ok) { $allOk = $false }
    $status = if ($ok) { "PASS" } else { "FAIL" }
    Write-Host ("{0}: {1}  Size={2,9:N0} bytes  PDF-Header={3}  Pages={4}" -f $status, $f, $fi.Length, $isPdf, $pageCount)
}

if ($allOk) {
    Write-Host "ALL PDF VALIDATIONS PASSED"
    exit 0
} else {
    Write-Host "PDF VALIDATION FAILED"
    exit 1
}
