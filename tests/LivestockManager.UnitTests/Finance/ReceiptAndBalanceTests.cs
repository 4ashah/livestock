using LivestockManager.Application.DTOs.Receipts;
using LivestockManager.Application.DTOs.Customers;
using LivestockManager.Application.Services.Receipts;
using LivestockManager.Application.Services.Customers;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace LivestockManager.UnitTests.Finance;

public class ReceiptAndBalanceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GenerateForPayment_CreatesReceiptWithNumber()
    {
        var scope = CreateTestScope(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var invoice = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 1000m,
                InvoiceNumber = "INV-001",
                Status = InvoiceStatus.Confirmed,
                Currency = Currency.USD
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var payment = new Payment(companyId, customerId, invoice.Id,
                new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 700m)
            {
                ProcessedByUserId = userId
            };
            db.Payments.Add(payment);
            invoice.Payments.Add(payment);
            invoice.RecalculatePaidAmountFromPayments();
            invoice.UpdateStatusFromBalances(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
            await db.SaveChangesAsync();

            var result = await receiptService.GenerateForPaymentAsync(payment.Id, "Payment received in cash", CancellationToken.None);

            Assert.NotNull(result);
            Assert.StartsWith("RCP-2026-", result.ReceiptNumber);
            Assert.EndsWith("00001", result.ReceiptNumber);
            Assert.Equal(700m, result.AmountReceived);
            Assert.Equal(ReceiptStatus.Issued, result.Status);
            Assert.Equal(userId, result.GeneratedByUserId);
            Assert.Equal("Payment received in cash", result.Notes);
            Assert.Equal(300m, result.RunningInvoiceBalance);
            Assert.NotNull(result.CustomerName);
        }
    }

    [Fact]
    public async Task GenerateForPayment_ThrowsIfPaymentReversed()
    {
        var scope = CreateTestScope(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var payment = new Payment(companyId, customerId, null,
                new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 500m);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            payment.ReversePayment(0m, "Test reversal", userId);
            await db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                receiptService.GenerateForPaymentAsync(payment.Id, null, CancellationToken.None));
            Assert.Contains("reversed payment", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ReverseReceipt_SetsStatusAndReversesPaymentAndAdjustsInvoiceBalance()
    {
        var fixedNow = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var scope = CreateTestScope(fixedNow);
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var invoice = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 1000m,
                InvoiceNumber = "INV-001",
                Status = InvoiceStatus.PartiallyPaid,
                Currency = Currency.USD
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var payment = new Payment(companyId, customerId, invoice.Id,
                new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 600m);
            db.Payments.Add(payment);
            invoice.Payments.Add(payment);
            invoice.RecalculatePaidAmountFromPayments();
            invoice.UpdateStatusFromBalances(fixedNow);
            await db.SaveChangesAsync();

            var receiptResult = await receiptService.GenerateForPaymentAsync(payment.Id, null, CancellationToken.None);
            var receiptId = receiptResult.Id;

            invoice = await db.Invoices.FirstAsync(i => i.Id == invoice.Id);
            Assert.Equal(600m, invoice.PaidAmount);

            var reversedBy = Guid.NewGuid();
            var beforeReverse = DateTimeOffset.UtcNow;
            var reversedResult = await receiptService.ReverseReceiptAsync(receiptId, "Customer returned goods", reversedBy, CancellationToken.None);
            var afterReverse = DateTimeOffset.UtcNow;

            Assert.Equal(ReceiptStatus.Reversed, reversedResult.Status);
            Assert.Equal("Customer returned goods", reversedResult.ReversalReason);
            Assert.Equal(reversedBy, reversedResult.ReversedByUserId);
            Assert.NotNull(reversedResult.ReversedAt);
            Assert.InRange(reversedResult.ReversedAt.Value, beforeReverse, afterReverse);

            var dbPayment = await db.Payments.FirstAsync(p => p.Id == payment.Id);
            Assert.True(dbPayment.IsReversed);
            Assert.Equal("Customer returned goods", dbPayment.ReversalReason);

            var dbInvoice = await db.Invoices.FirstAsync(i => i.Id == invoice.Id);
            Assert.Equal(0m, dbInvoice.PaidAmount);
            Assert.NotEqual(InvoiceStatus.Paid, dbInvoice.Status);
            Assert.NotEqual(InvoiceStatus.PartiallyPaid, dbInvoice.Status);
        }
    }

    [Fact]
    public async Task ReverseReceipt_ReasonEmpty_Throws()
    {
        var scope = CreateTestScope(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var invoice = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 500m,
                Status = InvoiceStatus.Confirmed
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var payment = new Payment(companyId, customerId, invoice.Id,
                new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 500m);
            db.Payments.Add(payment);
            invoice.Payments.Add(payment);
            invoice.RecalculatePaidAmountFromPayments();
            await db.SaveChangesAsync();

            var receipt = await receiptService.GenerateForPaymentAsync(payment.Id, null, CancellationToken.None);

            var ex1 = await Assert.ThrowsAsync<ArgumentException>(() =>
                receiptService.ReverseReceiptAsync(receipt.Id, "", Guid.NewGuid(), CancellationToken.None));
            Assert.Contains("reason", ex1.Message, StringComparison.OrdinalIgnoreCase);

            var ex2 = await Assert.ThrowsAsync<ArgumentException>(() =>
                receiptService.ReverseReceiptAsync(receipt.Id, "   ", Guid.NewGuid(), CancellationToken.None));
            Assert.Contains("reason", ex2.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task GetBalance_CalculatesCorrectTotals()
    {
        var fixedNow = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var scope = CreateTestScope(fixedNow);
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var inv1 = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 800m,
                Status = InvoiceStatus.PartiallyPaid
            };
            var inv2 = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 1200m,
                Status = InvoiceStatus.Confirmed
            };
            db.Invoices.AddRange(inv1, inv2);
            await db.SaveChangesAsync();

            var pay1 = new Payment(companyId, customerId, inv1.Id,
                new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 300m);
            db.Payments.Add(pay1);
            inv1.Payments.Add(pay1);
            inv1.RecalculatePaidAmountFromPayments();
            inv1.UpdateStatusFromBalances(fixedNow);
            inv2.UpdateStatusFromBalances(fixedNow);
            await db.SaveChangesAsync();

            var balance = await balanceService.GetBalanceAsync(companyId, customerId, CancellationToken.None);

            Assert.Equal(2000m, balance.TotalInvoiced);
            Assert.Equal(300m, balance.TotalPaid);
            Assert.Equal(1700m, balance.TotalOutstanding);
        }
    }

    [Fact]
    public async Task GetBalance_ReversalsExcludedFromPaid()
    {
        var fixedNow = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var scope = CreateTestScope(fixedNow);
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var inv = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 1000m,
                Status = InvoiceStatus.Confirmed
            };
            db.Invoices.Add(inv);
            await db.SaveChangesAsync();

            var goodPayment = new Payment(companyId, customerId, inv.Id,
                new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 400m);
            var reversedPayment = new Payment(companyId, customerId, inv.Id,
                new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cheque, 200m);
            db.Payments.AddRange(goodPayment, reversedPayment);
            inv.Payments.AddRange(new[] { goodPayment, reversedPayment });
            reversedPayment.ReversePayment(inv.GrandTotal, "Cheque bounced", userId);
            inv.RecalculatePaidAmountFromPayments();
            inv.UpdateStatusFromBalances(fixedNow);
            await db.SaveChangesAsync();

            var balance = await balanceService.GetBalanceAsync(companyId, customerId, CancellationToken.None);

            Assert.Equal(1000m, balance.TotalInvoiced);
            Assert.Equal(400m, balance.TotalPaid);
            Assert.Equal(600m, balance.TotalOutstanding);
        }
    }

    [Fact]
    public async Task GetBalance_TotalOverdueCorrectWhenDueDatePassed()
    {
        var fixedNow = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var scope = CreateTestScope(fixedNow);
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var overdueInv = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 500m,
                PaidAmount = 100m,
                Status = InvoiceStatus.Overdue
            };
            var notOverdueInv = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 800m,
                Status = InvoiceStatus.Confirmed
            };
            var cancelledInv = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 300m,
                Status = InvoiceStatus.Cancelled
            };
            db.Invoices.AddRange(overdueInv, notOverdueInv, cancelledInv);
            await db.SaveChangesAsync();

            var balance = await balanceService.GetBalanceAsync(companyId, customerId, CancellationToken.None);

            Assert.Equal(1300m, balance.TotalInvoiced);
            Assert.Equal(400m, balance.TotalOverdue);
        }
    }

    [Fact]
    public async Task ListBalances_ReturnsPerCustomer()
    {
        var fixedNow = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var scope = CreateTestScope(fixedNow);
        var (db, receiptService, balanceService, companyId, customerId1, userId) = scope;
        using (scope)
        {
            var cust2 = new Customer(companyId, "CUST-002", "Customer Two");
            db.Customers.Add(cust2);
            await db.SaveChangesAsync();
            var customerId2 = cust2.Id;

            var inv1 = new Invoice(companyId, customerId1,
                new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 600m,
                Status = InvoiceStatus.Confirmed
            };
            var inv2 = new Invoice(companyId, customerId2,
                new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 900m,
                Status = InvoiceStatus.Confirmed
            };
            db.Invoices.AddRange(inv1, inv2);
            await db.SaveChangesAsync();

            var pay = new Payment(companyId, customerId1, inv1.Id,
                new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 200m)
            {
                PaymentDate = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero)
            };
            db.Payments.Add(pay);
            inv1.Payments.Add(pay);
            inv1.RecalculatePaidAmountFromPayments();
            inv1.UpdateStatusFromBalances(fixedNow);
            inv2.UpdateStatusFromBalances(fixedNow);
            await db.SaveChangesAsync();

            var balances = await balanceService.ListBalancesAsync(companyId, CancellationToken.None);

            Assert.Equal(2, balances.Count);

            var bal1 = balances.First(b => b.CustomerId == customerId1);
            Assert.Equal(600m, bal1.TotalInvoiced);
            Assert.Equal(200m, bal1.TotalPaid);
            Assert.Equal(400m, bal1.TotalOutstanding);
            Assert.NotNull(bal1.LastPaymentDate);

            var bal2 = balances.First(b => b.CustomerId == customerId2);
            Assert.Equal(900m, bal2.TotalInvoiced);
            Assert.Equal(0m, bal2.TotalPaid);
            Assert.Equal(900m, bal2.TotalOutstanding);
            Assert.Null(bal2.LastPaymentDate);
        }
    }

    [Fact]
    public async Task GetStatement_OpeningAndClosingBalanceMatches()
    {
        var fixedNow = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero);
        var scope = CreateTestScope(fixedNow);
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var customer = await db.Customers.FirstAsync(c => c.Id == customerId);
            customer.OpeningBalance = 100m;
            await db.SaveChangesAsync();

            var priorInv = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 500m,
                InvoiceNumber = "INV-PRIOR",
                Status = InvoiceStatus.Confirmed
            };
            var priorPay = new Payment(companyId, customerId, priorInv.Id,
                new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 200m);
            db.Invoices.Add(priorInv);
            db.Payments.Add(priorPay);
            priorInv.Payments.Add(priorPay);
            priorInv.RecalculatePaidAmountFromPayments();
            await db.SaveChangesAsync();

            var rangeInv = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 800m,
                InvoiceNumber = "INV-001",
                Status = InvoiceStatus.Confirmed
            };
            var rangePay = new Payment(companyId, customerId, rangeInv.Id,
                new DateTimeOffset(2026, 5, 25, 0, 0, 0, TimeSpan.Zero),
                PaymentMethod.Cash, 300m);
            db.Invoices.Add(rangeInv);
            db.Payments.Add(rangePay);
            rangeInv.Payments.Add(rangePay);
            rangeInv.RecalculatePaidAmountFromPayments();
            await db.SaveChangesAsync();

            var fromDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            var toDate = new DateTimeOffset(2026, 5, 31, 0, 0, 0, TimeSpan.Zero);

            var statement = await balanceService.GetStatementAsync(companyId, customerId, fromDate, toDate, CancellationToken.None);

            var expectedOpening = (500m - 200m) + 100m;
            Assert.Equal(expectedOpening, statement.OpeningBalance);

            var totalDebit = statement.Lines.Sum(l => l.Debit);
            var totalCredit = statement.Lines.Sum(l => l.Credit);
            Assert.Equal(800m, totalDebit);
            Assert.Equal(300m, totalCredit);

            var expectedClosing = expectedOpening + totalDebit - totalCredit;
            Assert.Equal(expectedClosing, statement.ClosingBalance);

            if (statement.Lines.Count > 0)
            {
                var lastLine = statement.Lines.Last();
                Assert.Equal(expectedClosing, lastLine.RunningBalance);
            }
        }
    }

    [Fact]
    public async Task GetAgingBuckets_ReturnsAll5BucketsWithCorrectSums()
    {
        var fixedNow = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var scope = CreateTestScope(fixedNow);
        var (db, receiptService, balanceService, companyId, customerId, userId) = scope;
        using (scope)
        {
            var inv_0_30 = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 300m,
                PaidAmount = 0m,
                Status = InvoiceStatus.Confirmed
            };
            var inv_31_60 = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 4, 10, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 500m,
                PaidAmount = 100m,
                Status = InvoiceStatus.Overdue
            };
            var inv_61_90 = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 700m,
                PaidAmount = 0m,
                Status = InvoiceStatus.Overdue
            };
            var inv_91_180 = new Invoice(companyId, customerId,
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 900m,
                PaidAmount = 150m,
                Status = InvoiceStatus.Overdue
            };
            var inv_180plus = new Invoice(companyId, customerId,
                new DateTimeOffset(2025, 10, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 1200m,
                PaidAmount = 200m,
                Status = InvoiceStatus.Overdue
            };
            var cancelledNotCounted = new Invoice(companyId, customerId,
                new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 10, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 9999m,
                PaidAmount = 0m,
                Status = InvoiceStatus.Cancelled
            };
            var paidOffNotCounted = new Invoice(companyId, customerId,
                new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero))
            {
                GrandTotal = 400m,
                PaidAmount = 400m,
                Status = InvoiceStatus.Paid
            };
            db.Invoices.AddRange(inv_0_30, inv_31_60, inv_61_90, inv_91_180, inv_180plus, cancelledNotCounted, paidOffNotCounted);
            await db.SaveChangesAsync();

            var buckets = await balanceService.GetAgingBucketsAsync(companyId, customerId, CancellationToken.None);

            Assert.Equal(5, buckets.Count);
            var b0 = buckets.First(b => b.BucketName == "0-30");
            var b1 = buckets.First(b => b.BucketName == "31-60");
            var b2 = buckets.First(b => b.BucketName == "61-90");
            var b3 = buckets.First(b => b.BucketName == "91-180");
            var b4 = buckets.First(b => b.BucketName == "180+");

            Assert.Equal(300m, b0.Amount);
            Assert.Equal(400m, b1.Amount);
            Assert.Equal(700m, b2.Amount);
            Assert.Equal(750m, b3.Amount);
            Assert.Equal(1000m, b4.Amount);
        }
    }

    private static FinanceTestScope CreateTestScope(DateTimeOffset? fixedNow = null)
    {
        var db = CreateInMemoryDb();
        var company = new Company("Test Co") { Currency = Currency.USD, InvoicePrefix = "INV", ReceiptPrefix = "RCT" };
        db.Companies.Add(company);
        var customer = new Customer(company.Id, "CUST-001", "Test Customer")
        {
            BillingAddress = new Domain.ValueObjects.Address("123 Main St", "Apt 4B", "Testville", "TS", "12345", "US"),
            TaxNumber = "TX-123456"
        };
        db.Customers.Add(customer);
        db.SaveChanges();

        var userId = Guid.NewGuid();
        var now = fixedNow ?? new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        IDateTime dt = new TestFinanceDateTime { Now = now };
        var seqGen = new MockSequenceGenerator();
        var receiptService = new ReceiptService(db, dt, seqGen);
        var balanceService = new CustomerBalanceService(db, dt);
        return new FinanceTestScope(db, receiptService, balanceService, company.Id, customer.Id, userId);
    }
}

public sealed class FinanceTestScope : IDisposable
{
    public AppDbContext Db { get; }
    public ReceiptService ReceiptService { get; }
    public CustomerBalanceService BalanceService { get; }
    public Guid CompanyId { get; }
    public Guid CustomerId { get; }
    public Guid UserId { get; }

    public FinanceTestScope(AppDbContext db, ReceiptService receiptService, CustomerBalanceService balanceService, Guid companyId, Guid customerId, Guid userId)
    {
        Db = db;
        ReceiptService = receiptService;
        BalanceService = balanceService;
        CompanyId = companyId;
        CustomerId = customerId;
        UserId = userId;
    }

    public void Deconstruct(out AppDbContext db, out ReceiptService receiptService, out CustomerBalanceService balanceService, out Guid companyId, out Guid customerId, out Guid userId)
    {
        db = Db;
        receiptService = ReceiptService;
        balanceService = BalanceService;
        companyId = CompanyId;
        customerId = CustomerId;
        userId = UserId;
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}

public class TestFinanceDateTime : IDateTime
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
}

public class MockSequenceGenerator : ISequenceGenerator
{
    private readonly ConcurrentDictionary<string, long> _counters = new();

    public Task<string> GenerateLivestockIdAsync(Guid companyId, LivestockType type)
    {
        throw new NotImplementedException();
    }

    public Task<string> GenerateInvoiceNumberAsync(Guid companyId)
    {
        throw new NotImplementedException();
    }

    public Task<string> GenerateReceiptNumberAsync(Guid companyId)
    {
        throw new NotImplementedException();
    }

    public Task<string> GenerateDocumentNumberAsync(Guid companyId, string prefix)
    {
        var key = $"{companyId}-{prefix}";
        var next = _counters.AddOrUpdate(key, 1, (_, old) => old + 1);
        return Task.FromResult(prefix + next.ToString("D5"));
    }
}
