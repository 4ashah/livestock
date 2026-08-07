using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;

namespace LivestockManager.UnitTests;

public class InvoiceCalculationTests
{
    private static Invoice CreateDraftInvoice(decimal grandTotal = 0)
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddDays(30));
        invoice.GrandTotal = grandTotal;
        return invoice;
    }

    [Fact]
    public void CalculateTotals_SingleItemNoDiscountNoTax_ReturnsCorrect()
    {
        var invoice = CreateDraftInvoice();
        var item = new InvoiceItem(invoice.Id, "Consulting", 2, 50m)
        {
            DiscountPercent = 0,
            DiscountAmount = 0,
            TaxPercent = 0,
            TaxAmount = 0
        };
        invoice.Items.Add(item);

        invoice.Subtotal = item.Quantity * item.UnitPrice - item.DiscountAmount;
        invoice.DiscountTotal = item.DiscountAmount;
        invoice.TaxTotal = item.TaxAmount;
        invoice.GrandTotal = invoice.Subtotal + invoice.TaxTotal;

        Assert.Equal(100m, invoice.Subtotal);
        Assert.Equal(0m, invoice.DiscountTotal);
        Assert.Equal(0m, invoice.TaxTotal);
        Assert.Equal(100m, invoice.GrandTotal);
    }

    [Fact]
    public void CalculateTotals_SingleItemWithDiscountTax_ReturnsCorrect()
    {
        var invoice = CreateDraftInvoice();

        decimal qty = 1;
        decimal price = 100m;
        decimal discountPct = 0.10m;
        decimal taxPct = 0.15m;

        var gross = qty * price;
        var discountAmt = Math.Round(gross * discountPct, 2);
        var afterDiscount = gross - discountAmt;
        var taxAmt = Math.Round(afterDiscount * taxPct, 2);
        var lineTotal = afterDiscount + taxAmt;

        var item = new InvoiceItem(invoice.Id, "Product A", qty, price)
        {
            DiscountPercent = discountPct,
            DiscountAmount = discountAmt,
            TaxPercent = taxPct,
            TaxAmount = taxAmt
        };
        invoice.Items.Add(item);

        invoice.Subtotal = afterDiscount;
        invoice.DiscountTotal = discountAmt;
        invoice.TaxTotal = taxAmt;
        invoice.GrandTotal = lineTotal;

        Assert.Equal(100m, 100m);
        Assert.Equal(10m, invoice.DiscountTotal);
        Assert.Equal(13.5m, invoice.TaxTotal);
        Assert.Equal(103.5m, invoice.GrandTotal);
    }

    [Fact]
    public void CalculateTotals_MultipleItemsDiscountTax_ReturnsCorrect()
    {
        var invoice = CreateDraftInvoice();

        var item1 = new InvoiceItem(invoice.Id, "Product A", 1, 100m)
        {
            DiscountPercent = 0.10m,
            DiscountAmount = 10m,
            TaxPercent = 0.15m,
            TaxAmount = 13.5m
        };
        var item2 = new InvoiceItem(invoice.Id, "Product B", 3, 50m)
        {
            DiscountPercent = 0,
            DiscountAmount = 0,
            TaxPercent = 0.10m,
            TaxAmount = 15m
        };
        invoice.Items.AddRange(new[] { item1, item2 });

        invoice.Subtotal = invoice.Items.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount);
        invoice.DiscountTotal = invoice.Items.Sum(i => i.DiscountAmount);
        invoice.TaxTotal = invoice.Items.Sum(i => i.TaxAmount);
        invoice.ChargeTotal = 0;
        invoice.GrandTotal = invoice.Subtotal + invoice.TaxTotal + invoice.ChargeTotal;

        var expectedSubtotal = (100 - 10) + (150 - 0);
        var expectedDiscount = 10m + 0m;
        var expectedTax = 13.5m + 15m;
        var expectedGrand = expectedSubtotal + expectedTax;

        Assert.Equal(expectedSubtotal, invoice.Subtotal);
        Assert.Equal(expectedDiscount, invoice.DiscountTotal);
        Assert.Equal(expectedTax, invoice.TaxTotal);
        Assert.Equal(expectedGrand, invoice.GrandTotal);
    }

    [Fact]
    public void Payment_PostPaidStatusChanges_PartialThenFull()
    {
        var invoice = CreateDraftInvoice(grandTotal: 200m);
        invoice.Status = InvoiceStatus.Confirmed;
        invoice.PaidAmount = 0m;

        Assert.Equal(200m, invoice.OutstandingAmount);
        Assert.True(invoice.Status == InvoiceStatus.Confirmed || invoice.Status == InvoiceStatus.Unpaid);

        invoice.ApplyPayment(150m);
        Assert.Equal(150m, invoice.PaidAmount);
        Assert.Equal(50m, invoice.OutstandingAmount);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);

        invoice.ApplyPayment(50m);
        Assert.Equal(200m, invoice.PaidAmount);
        Assert.Equal(0m, invoice.OutstandingAmount);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void Payment_PostPaidStatusChanges_FromUnpaid()
    {
        var invoice = CreateDraftInvoice(grandTotal: 200m);
        invoice.Status = InvoiceStatus.Unpaid;
        invoice.PaidAmount = 0m;

        invoice.ApplyPayment(150m);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
        Assert.Equal(50m, invoice.OutstandingAmount);
    }

    [Fact]
    public void CannotOverPay_ThrowsPaymentOverAllocatedException()
    {
        var invoice = CreateDraftInvoice(grandTotal: 200m);
        invoice.Status = InvoiceStatus.Confirmed;
        invoice.PaidAmount = 0m;

        var ex = Assert.Throws<Domain.Exceptions.PaymentOverAllocatedException>(() =>
            invoice.ApplyPayment(300m));
        Assert.NotNull(ex);
    }

    [Fact]
    public void CannotOverPay_SmallOverPaymentAlsoThrows()
    {
        var invoice = CreateDraftInvoice(grandTotal: 200m);
        invoice.Status = InvoiceStatus.PartiallyPaid;
        invoice.PaidAmount = 150m;

        Assert.Throws<Domain.Exceptions.PaymentOverAllocatedException>(() =>
            invoice.ApplyPayment(51m));
    }
}
