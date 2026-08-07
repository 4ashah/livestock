using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.UnitTests.Finance;

public class InvoiceAdditionalChargeTests
{
    private static Invoice CreateDraftInvoice()
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddDays(30));
        return invoice;
    }

    [Fact]
    public void UpdateTotalsFromItemsAndCharges_ThreeItemsTwoCharges_VerifiesAllTotals()
    {
        var invoice = CreateDraftInvoice();
        invoice.DiscountTotal = 0m;

        var item1 = new InvoiceItem(invoice.Id, "Cattle - Angus Bull", 1m, 2500.00m)
        {
            DiscountPercent = 0m,
            TaxPercent = 0.10m
        };
        var item2 = new InvoiceItem(invoice.Id, "Cattle - Angus Cow", 2m, 1800.00m)
        {
            DiscountPercent = 0.05m,
            TaxPercent = 0.10m
        };
        var item3 = new InvoiceItem(invoice.Id, "Vaccination - Bovine", 5m, 45.00m)
        {
            DiscountPercent = 0m,
            TaxPercent = 0.08m
        };
        invoice.Items.AddRange(new[] { item1, item2, item3 });

        var charge1 = new InvoiceAdditionalCharge(invoice.Id, "Delivery Fee", 120.00m)
        {
            TaxAmount = 9.60m
        };
        var charge2 = new InvoiceAdditionalCharge(invoice.Id, "Insurance Surcharge", 75.50m)
        {
            TaxAmount = 6.04m
        };
        invoice.AdditionalCharges.AddRange(new[] { charge1, charge2 });

        invoice.UpdateTotalsFromItemsAndCharges();

        decimal item1Net = Math.Round(2500.00m * 1m * (1m - 0m), 2, MidpointRounding.AwayFromZero);
        decimal item2Net = Math.Round(1800.00m * 2m * (1m - 0.05m), 2, MidpointRounding.AwayFromZero);
        decimal item3Net = Math.Round(45.00m * 5m * (1m - 0m), 2, MidpointRounding.AwayFromZero);
        decimal expectedSubtotal = item1Net + item2Net + item3Net;

        decimal item1Tax = Math.Round(item1Net * 0.10m, 2, MidpointRounding.AwayFromZero);
        decimal item2Tax = Math.Round(item2Net * 0.10m, 2, MidpointRounding.AwayFromZero);
        decimal item3Tax = Math.Round(item3Net * 0.08m, 2, MidpointRounding.AwayFromZero);
        decimal expectedTaxTotal = item1Tax + item2Tax + item3Tax + 9.60m + 6.04m;

        decimal expectedChargeTotal = (120.00m + 9.60m) + (75.50m + 6.04m);
        decimal expectedGrandTotal = expectedSubtotal - 0m + expectedTaxTotal + expectedChargeTotal;

        Assert.Equal(expectedSubtotal, invoice.Subtotal);
        Assert.Equal(expectedChargeTotal, invoice.ChargeTotal);
        Assert.Equal(expectedTaxTotal, invoice.TaxTotal);
        Assert.Equal(expectedGrandTotal, invoice.GrandTotal);
        Assert.Equal(expectedGrandTotal - invoice.PaidAmount, invoice.OutstandingAmount);
    }

    [Fact]
    public void UpdateTotalsFromItemsAndCharges_WithHeaderDiscount_SubtractsFromGrandTotal()
    {
        var invoice = CreateDraftInvoice();
        invoice.DiscountTotal = 50.00m;

        var item = new InvoiceItem(invoice.Id, "Hay Bale", 10m, 25.00m)
        {
            DiscountPercent = 0m,
            TaxPercent = 0.05m
        };
        invoice.Items.Add(item);

        invoice.UpdateTotalsFromItemsAndCharges();

        decimal expectedSubtotal = Math.Round(25.00m * 10m, 2, MidpointRounding.AwayFromZero);
        decimal expectedTax = Math.Round(expectedSubtotal * 0.05m, 2, MidpointRounding.AwayFromZero);
        decimal expectedGrand = expectedSubtotal - 50.00m + expectedTax + 0m;

        Assert.Equal(expectedSubtotal, invoice.Subtotal);
        Assert.Equal(expectedGrand, invoice.GrandTotal);
    }

    [Fact]
    public void UpdateTotalsFromItemsAndCharges_EmptyItemsAndCharges_AllZeros()
    {
        var invoice = CreateDraftInvoice();

        invoice.UpdateTotalsFromItemsAndCharges();

        Assert.Equal(0m, invoice.Subtotal);
        Assert.Equal(0m, invoice.ChargeTotal);
        Assert.Equal(0m, invoice.TaxTotal);
        Assert.Equal(0m, invoice.GrandTotal);
    }

    [Fact]
    public void UpdateStatusFromBalances_CancelledOrVoided_Unchanged()
    {
        var invoice = CreateDraftInvoice();
        invoice.GrandTotal = 100m;
        invoice.PaidAmount = 0m;

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.UpdateStatusFromBalances(DateTimeOffset.Now);
        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);

        invoice.Status = InvoiceStatus.Voided;
        invoice.UpdateStatusFromBalances(DateTimeOffset.Now);
        Assert.Equal(InvoiceStatus.Voided, invoice.Status);
    }

    [Fact]
    public void UpdateStatusFromBalances_ZeroPaidNotDraft_BecomesConfirmed()
    {
        var invoice = CreateDraftInvoice();
        invoice.GrandTotal = 500m;
        invoice.PaidAmount = 0m;
        invoice.Status = InvoiceStatus.Unpaid;

        invoice.UpdateStatusFromBalances(DateTimeOffset.Now, allowOverdueMarker: false);

        Assert.Equal(InvoiceStatus.Confirmed, invoice.Status);
    }

    [Fact]
    public void UpdateStatusFromBalances_PartialPayment_BecomesPartiallyPaid()
    {
        var invoice = CreateDraftInvoice();
        invoice.GrandTotal = 500m;
        invoice.PaidAmount = 200m;
        invoice.Status = InvoiceStatus.Confirmed;

        invoice.UpdateStatusFromBalances(DateTimeOffset.Now, allowOverdueMarker: false);

        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
    }

    [Fact]
    public void UpdateStatusFromBalances_FullPayment_BecomesPaid()
    {
        var invoice = CreateDraftInvoice();
        invoice.GrandTotal = 500m;
        invoice.PaidAmount = 500m;
        invoice.Status = InvoiceStatus.PartiallyPaid;

        invoice.UpdateStatusFromBalances(DateTimeOffset.Now, allowOverdueMarker: false);

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void UpdateStatusFromBalances_OverdueWithOutstanding_BecomesOverdue()
    {
        var invoice = CreateDraftInvoice();
        invoice.GrandTotal = 500m;
        invoice.PaidAmount = 0m;
        invoice.Status = InvoiceStatus.Confirmed;
        invoice.DueDate = DateTimeOffset.Now.AddDays(-10);

        invoice.UpdateStatusFromBalances(DateTimeOffset.Now, allowOverdueMarker: true);

        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    [Fact]
    public void UpdateStatusFromBalances_OverdueDisabled_DoesNotMarkOverdue()
    {
        var invoice = CreateDraftInvoice();
        invoice.GrandTotal = 500m;
        invoice.PaidAmount = 0m;
        invoice.Status = InvoiceStatus.Confirmed;
        invoice.DueDate = DateTimeOffset.Now.AddDays(-10);

        invoice.UpdateStatusFromBalances(DateTimeOffset.Now, allowOverdueMarker: false);

        Assert.Equal(InvoiceStatus.Confirmed, invoice.Status);
    }

    [Fact]
    public void SnapshotCustomer_SerializesNameTaxAddress()
    {
        var invoice = CreateDraftInvoice();
        var customer = new Customer(invoice.CompanyId, "CUST-001", "Smith Cattle Co")
        {
            TaxNumber = "TX-12345",
            BillingAddress = new Address("123 Ranch Rd", "Suite A", "Austin", "TX", "78701", "USA")
        };

        invoice.SnapshotCustomer(customer);

        Assert.NotNull(invoice.CustomerSnapshot);
        var parts = invoice.CustomerSnapshot.Split('|');
        Assert.Equal(8, parts.Length);
        Assert.Equal("Smith Cattle Co", parts[0]);
        Assert.Equal("TX-12345", parts[1]);
        Assert.Equal("123 Ranch Rd", parts[2]);
        Assert.Equal("Suite A", parts[3]);
        Assert.Equal("Austin", parts[4]);
        Assert.Equal("TX", parts[5]);
        Assert.Equal("78701", parts[6]);
        Assert.Equal("USA", parts[7]);
    }

    [Fact]
    public void SnapshotCompany_SerializesNameTaxAddress()
    {
        var invoice = CreateDraftInvoice();
        var company = new Company("Big Ranch Ltd")
        {
            TaxNumber = "CO-99887",
            Address = new Address("456 Farm Ln", null, "Dallas", "TX", "75201", "USA")
        };

        invoice.SnapshotCompany(company);

        Assert.NotNull(invoice.CompanySnapshot);
        var parts = invoice.CompanySnapshot.Split('|');
        Assert.Equal(8, parts.Length);
        Assert.Equal("Big Ranch Ltd", parts[0]);
        Assert.Equal("CO-99887", parts[1]);
        Assert.Equal("456 Farm Ln", parts[2]);
        Assert.Equal(string.Empty, parts[3]);
        Assert.Equal("Dallas", parts[4]);
        Assert.Equal("TX", parts[5]);
        Assert.Equal("75201", parts[6]);
        Assert.Equal("USA", parts[7]);
    }

    [Fact]
    public void SnapshotCustomer_NullInput_SetsNull()
    {
        var invoice = CreateDraftInvoice();
        invoice.CustomerSnapshot = "existing";

        invoice.SnapshotCustomer(null!);

        Assert.Null(invoice.CustomerSnapshot);
    }

    [Fact]
    public void SnapshotCompany_NullInput_SetsNull()
    {
        var invoice = CreateDraftInvoice();
        invoice.CompanySnapshot = "existing";

        invoice.SnapshotCompany(null!);

        Assert.Null(invoice.CompanySnapshot);
    }

    [Fact]
    public void RecalculatePaidAmountFromPayments_SumsContributedAmounts()
    {
        var invoice = CreateDraftInvoice();
        invoice.GrandTotal = 1000m;

        var p1 = new Payment(invoice.CompanyId, invoice.CustomerId, invoice.Id, DateTimeOffset.Now, PaymentMethod.Cash, 300m);
        var p2 = new Payment(invoice.CompanyId, invoice.CustomerId, invoice.Id, DateTimeOffset.Now, PaymentMethod.BankTransfer, 200m);
        var p3 = new Payment(invoice.CompanyId, invoice.CustomerId, invoice.Id, DateTimeOffset.Now, PaymentMethod.Cheque, 100m);
        p3.IsReversed = true;

        invoice.Payments.AddRange(new[] { p1, p2, p3 });
        invoice.RecalculatePaidAmountFromPayments();

        Assert.Equal(500m, invoice.PaidAmount);
    }
}
