using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;

namespace LivestockManager.UnitTests.Finance;

public class PaymentReversalTests
{
    private static Payment CreatePayment(decimal amount = 500m, Guid? invoiceId = null)
    {
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        return new Payment(companyId, customerId, invoiceId, DateTimeOffset.Now, PaymentMethod.Cash, amount);
    }

    [Fact]
    public void ReversePayment_SetsFieldsCorrectly()
    {
        var payment = CreatePayment(750m);
        var reason = "Duplicate payment - entered twice";
        var reversedBy = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        payment.ReversePayment(1000m, reason, reversedBy);

        var after = DateTimeOffset.UtcNow;

        Assert.True(payment.IsReversed);
        Assert.Equal(reason, payment.ReversalReason);
        Assert.Equal(reversedBy, payment.ReversedByUserId);
        Assert.NotNull(payment.ReversedAt);
        Assert.InRange(payment.ReversedAt.Value, before, after);
        Assert.Equal(750m, payment.Amount);
    }

    [Fact]
    public void ReversePayment_AlreadyReversed_ThrowsInvalidOperationException()
    {
        var payment = CreatePayment(200m);
        payment.ReversePayment(500m, "First reason", Guid.NewGuid());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            payment.ReversePayment(500m, "Second reason", Guid.NewGuid()));

        Assert.Equal("Payment is already reversed.", ex.Message);
    }

    [Fact]
    public void ReversePayment_EmptyReason_ThrowsArgumentException()
    {
        var payment = CreatePayment(300m);

        Assert.Throws<ArgumentException>(() =>
            payment.ReversePayment(500m, "", Guid.NewGuid()));
    }

    [Fact]
    public void ReversePayment_WhitespaceReason_ThrowsArgumentException()
    {
        var payment = CreatePayment(300m);

        Assert.Throws<ArgumentException>(() =>
            payment.ReversePayment(500m, "   ", Guid.NewGuid()));
    }

    [Fact]
    public void ReversePayment_NullReason_ThrowsArgumentException()
    {
        var payment = CreatePayment(300m);

        Assert.Throws<ArgumentException>(() =>
            payment.ReversePayment(500m, null!, Guid.NewGuid()));
    }

    [Fact]
    public void GetContributedAmount_NotReversed_ReturnsAmount()
    {
        var payment = CreatePayment(425.75m);

        Assert.False(payment.IsReversed);
        Assert.Equal(425.75m, payment.GetContributedAmount());
    }

    [Fact]
    public void GetContributedAmount_Reversed_ReturnsZero()
    {
        var payment = CreatePayment(425.75m);
        payment.ReversePayment(1000m, "Customer refund", Guid.NewGuid());

        Assert.True(payment.IsReversed);
        Assert.Equal(0m, payment.GetContributedAmount());
    }

    [Fact]
    public void GetContributedAmount_MultiplePaymentsSomeReversed_SumsCorrectly()
    {
        var payments = new List<Payment>
        {
            CreatePayment(100m),
            CreatePayment(200m),
            CreatePayment(50m),
            CreatePayment(300m)
        };
        payments[1].ReversePayment(1000m, "NSF cheque returned", Guid.NewGuid());
        payments[3].ReversePayment(1000m, "Posted to wrong invoice", Guid.NewGuid());

        decimal total = payments.Sum(p => p.GetContributedAmount());

        Assert.Equal(150m, total);
    }

    [Fact]
    public void ReversePayment_AmountFieldUnchanged()
    {
        var payment = CreatePayment(999.99m);

        payment.ReversePayment(2000m, "Reason", Guid.NewGuid());

        Assert.Equal(999.99m, payment.Amount);
    }

    [Fact]
    public void ReversePayment_WithChequeMethod_Works()
    {
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var payment = new Payment(companyId, customerId, null, DateTimeOffset.Now, PaymentMethod.Cheque, 1234.56m);

        payment.ReversePayment(5000m, "Cheque bounced", Guid.NewGuid());

        Assert.True(payment.IsReversed);
        Assert.Equal(PaymentMethod.Cheque, payment.Method);
        Assert.Equal(1234.56m, payment.Amount);
        Assert.Equal(0m, payment.GetContributedAmount());
    }

    [Fact]
    public void ReversePayment_WithMobilePaymentMethod_Works()
    {
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var payment = new Payment(companyId, customerId, null, DateTimeOffset.Now, PaymentMethod.MobilePayment, 88.88m);

        payment.ReversePayment(500m, "Mobile payment reversed by processor", Guid.NewGuid());

        Assert.True(payment.IsReversed);
        Assert.Equal(PaymentMethod.MobilePayment, payment.Method);
        Assert.Equal(0m, payment.GetContributedAmount());
    }

    [Fact]
    public void Invoice_AfterPaymentReversal_RecalculatesPaidAmount()
    {
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoice = new Invoice(companyId, customerId, DateTimeOffset.Now, DateTimeOffset.Now.AddDays(30))
        {
            GrandTotal = 1000m
        };
        var invoiceId = invoice.Id;

        var p1 = new Payment(companyId, customerId, invoiceId, DateTimeOffset.Now, PaymentMethod.Cash, 400m);
        var p2 = new Payment(companyId, customerId, invoiceId, DateTimeOffset.Now, PaymentMethod.BankTransfer, 300m);
        invoice.Payments.AddRange(new[] { p1, p2 });

        invoice.RecalculatePaidAmountFromPayments();
        Assert.Equal(700m, invoice.PaidAmount);

        p1.ReversePayment(1000m, "Cash count discrepancy", Guid.NewGuid());
        invoice.RecalculatePaidAmountFromPayments();

        Assert.Equal(300m, invoice.PaidAmount);
    }
}
