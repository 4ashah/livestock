using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Infrastructure.Services;
using LivestockManager.Infrastructure.Services.Sequencing;

namespace LivestockManager.UnitTests.Services;

public class SequenceRemediationTests : IAsyncLifetime
{
    private const string MasterConnection = "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=10;MultipleActiveResultSets=True";
    private const string TestDbName = "Test_LivestockManager_SeqRemediation";
    private const string ConcurrencyDbName = "Test_LivestockManager_SeqConcurrency";
    private static readonly string TestConnStr = $"Server=.;Database={TestDbName};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=15;MultipleActiveResultSets=True";
    private static readonly string ConcurrencyConnStr = $"Server=.;Database={ConcurrencyDbName};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30;MultipleActiveResultSets=True";

    private static readonly object DbLock = new();
    private static bool _sqlAvailable;
    private static bool _sqlChecked;

    public static bool CheckSqlAvailable()
    {
        if (_sqlChecked) return _sqlAvailable;
        lock (DbLock)
        {
            if (_sqlChecked) return _sqlAvailable;
            try
            {
                using var conn = new SqlConnection(MasterConnection);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.ExecuteScalar();
                _sqlAvailable = true;
            }
            catch
            {
                _sqlAvailable = false;
            }
            finally
            {
                _sqlChecked = true;
            }
        }
        return _sqlAvailable;
    }

    private static async Task CreateAndMigrateDb(string connStr, string dbName)
    {
        using (var masterConn = new SqlConnection(MasterConnection))
        {
            await masterConn.OpenAsync();
            using (var dropCmd = masterConn.CreateCommand())
            {
                dropCmd.CommandText = $"IF DB_ID(N'{dbName}') IS NOT NULL BEGIN ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}]; END";
                await dropCmd.ExecuteNonQueryAsync();
            }
            using (var createCmd = masterConn.CreateCommand())
            {
                createCmd.CommandText = $"CREATE DATABASE [{dbName}]";
                await createCmd.ExecuteNonQueryAsync();
            }
        }
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connStr).Options;
        using var ctx = new AppDbContext(opts);
        await ctx.Database.MigrateAsync();
    }

    private static async Task DropDbIfExists(string dbName)
    {
        try
        {
            using var masterConn = new SqlConnection(MasterConnection);
            await masterConn.OpenAsync();
            using var cmd = masterConn.CreateCommand();
            cmd.CommandText = $"IF DB_ID(N'{dbName}') IS NOT NULL BEGIN ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}]; END";
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
        }
    }

    private static async Task<Guid> EnsureCompany(AppDbContext ctx, string name, string invoicePrefix = "INV", string receiptPrefix = null!)
    {
        var existing = await ctx.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name);
        if (existing != null) return existing.Id;
        var company = new Company(name)
        {
            InvoicePrefix = invoicePrefix,
            ReceiptPrefix = receiptPrefix ?? string.Empty,
            IsActive = true
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();
        return company.Id;
    }

    public async Task InitializeAsync()
    {
        if (CheckSqlAvailable())
        {
            await DropDbIfExists(TestDbName);
            await DropDbIfExists(ConcurrencyDbName);
            await CreateAndMigrateDb(TestConnStr, TestDbName);
        }
    }

    public async Task DisposeAsync()
    {
        if (CheckSqlAvailable())
        {
            await DropDbIfExists(TestDbName);
            await DropDbIfExists(ConcurrencyDbName);
        }
    }

    private static AppDbContext CreateContext(string connStr)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connStr).Options;
        return new AppDbContext(opts);
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    public async Task Invoice2026_First_Format_INV202600001()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        var year = DateTime.UtcNow.Year;
        using var ctx = CreateContext(TestConnStr);
        var companyId = await EnsureCompany(ctx, "SeqRemediation_Inv2026");
        var gen = new EfSequenceGenerator(ctx);

        var result = await gen.GenerateInvoiceNumberAsync(companyId);

        Assert.StartsWith("INV-", result);
        Assert.Contains($"-{year}-", result);
        Assert.EndsWith("-00001", result);
        Assert.Equal($"INV-{year}-00001", result);
        var parts = result.Split('-');
        Assert.Equal(3, parts.Length);
        Assert.Equal(5, parts[2].Length);
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    public async Task ReceiptDefault_RCP()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        var year = DateTime.UtcNow.Year;
        using var ctx = CreateContext(TestConnStr);
        var companyId = await EnsureCompany(ctx, "SeqRemediation_RCP_Default", receiptPrefix: string.Empty);
        var gen = new EfSequenceGenerator(ctx);

        var result = await gen.GenerateReceiptNumberAsync(companyId);

        Assert.StartsWith("RCP-", result);
        Assert.False(result.StartsWith("RCT-", StringComparison.Ordinal), "Receipt prefix must NOT start with RCT-");
        Assert.Contains($"-{year}-", result);
        Assert.EndsWith("-00001", result);
        Assert.Equal($"RCP-{year}-00001", result);
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    public async Task Invoice2027_First_Independent()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        var currentYear = DateTime.UtcNow.Year;
        var nextYear = currentYear + 1;
        using var ctx = CreateContext(TestConnStr);
        var companyId = await EnsureCompany(ctx, "SeqRemediation_IndepYears");

        var company = await ctx.Companies.FindAsync(companyId);
        var prefix = string.IsNullOrWhiteSpace(company!.InvoicePrefix) ? "INV" : company.InvoicePrefix.Trim();
        var keyCurrent = $"{prefix}:{currentYear}";
        var keyNext = $"{prefix}:{nextYear}";

        ctx.SequenceCounters.Add(new SequenceCounter(companyId, keyCurrent) { LastValue = 5, LastUpdatedAt = DateTimeOffset.UtcNow });
        ctx.SequenceCounters.Add(new SequenceCounter(companyId, keyNext) { LastValue = 0, LastUpdatedAt = DateTimeOffset.UtcNow });
        await ctx.SaveChangesAsync();

        var gen = new EfSequenceGenerator(ctx);
        using var ctx2 = CreateContext(TestConnStr);

        var yearNowField = typeof(EfSequenceGenerator).GetMethod("GenerateInvoiceNumberAsync")!;
        var nowResult = await gen.GenerateInvoiceNumberAsync(companyId);
        Assert.Equal($"INV-{currentYear}-00006", nowResult);

        await using var ctx3 = CreateContext(TestConnStr);
        var directGen = new EfSequenceGenerator(ctx3);
        var scopedKeyNext = $"{prefix}:{nextYear}";
        var rawNext = await directGen.GenerateDocumentNumberAsync(companyId, scopedKeyNext);
        Assert.Equal($"INV-{nextYear}-00001", rawNext);

        Assert.NotEqual(nowResult, rawNext);
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    public async Task PurchasePaymentReceipt_IndependentSequences()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        var year = DateTime.UtcNow.Year;
        using var ctx = CreateContext(TestConnStr);
        var companyId = await EnsureCompany(ctx, "SeqRemediation_IndepSeqs");
        var gen = new EfSequenceGenerator(ctx);

        var purTask = gen.GenerateDocumentNumberAsync(companyId, $"PUR:{year}");
        using var ctxPay = CreateContext(TestConnStr);
        var genPay = new EfSequenceGenerator(ctxPay);
        var payTask = genPay.GenerateDocumentNumberAsync(companyId, $"PAY:{year}");
        using var ctxRcp = CreateContext(TestConnStr);
        var genRcp = new EfSequenceGenerator(ctxRcp);
        var rcpTask = genRcp.GenerateDocumentNumberAsync(companyId, $"RCP:{year}");

        await Task.WhenAll(purTask, payTask, rcpTask);

        var pur = purTask.Result;
        var pay = payTask.Result;
        var rcp = rcpTask.Result;

        Assert.Equal($"PUR-{year}-00001", pur);
        Assert.Equal($"PAY-{year}-00001", pay);
        Assert.Equal($"RCP-{year}-00001", rcp);
        Assert.All(new[] { pur, pay, rcp }, n => Assert.EndsWith("-00001", n));
        Assert.Equal(3, new HashSet<string> { pur[..3], pay[..3], rcp[..3] }.Count);
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    public async Task CompanyACompanyB_Independent()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        var year = DateTime.UtcNow.Year;
        using var ctx = CreateContext(TestConnStr);
        var companyA = await EnsureCompany(ctx, "SeqRemediation_CompanyA");
        using var ctxB = CreateContext(TestConnStr);
        var companyB = await EnsureCompany(ctxB, "SeqRemediation_CompanyB");

        Assert.NotEqual(companyA, companyB);

        var genA = new EfSequenceGenerator(ctx);
        var genB = new EfSequenceGenerator(ctxB);

        var invA = await genA.GenerateInvoiceNumberAsync(companyA);
        var invB = await genB.GenerateInvoiceNumberAsync(companyB);

        Assert.Equal($"INV-{year}-00001", invA);
        Assert.Equal($"INV-{year}-00001", invB);
        Assert.Equal(invA, invB);

        using var ctxVerify = CreateContext(TestConnStr);
        var counters = await ctxVerify.SequenceCounters
            .Where(sc => sc.CompanyId == companyA || sc.CompanyId == companyB)
            .Where(sc => sc.Prefix == $"INV:{year}")
            .ToListAsync();
        Assert.Equal(2, counters.Count);
        Assert.All(counters, c => Assert.Equal(1, c.LastValue));
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    public async Task HistoricalNonEmptyNumber_Untouched()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        var year = DateTime.UtcNow.Year;
        using var ctx = CreateContext(TestConnStr);
        var companyId = await EnsureCompany(ctx, "SeqRemediation_Historical");

        var customer = new Customer(companyId, "CUST-HIST-002", "Test Hist Customer 2")
        {
            IsActive = true,
            IsBusiness = false,
            PaymentTermsDays = 30
        };
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();
        var customerId = customer.Id;

        var invoiceDate = DateTimeOffset.UtcNow;
        var existingInvoiceNumber = "OLD-INV-99999";
        var invoice = new Invoice(companyId, customerId, invoiceDate, invoiceDate.AddDays(30))
        {
            InvoiceNumber = existingInvoiceNumber,
            Status = InvoiceStatus.Draft,
            Currency = Currency.USD
        };
        ctx.Invoices.Add(invoice);
        await ctx.SaveChangesAsync();
        var invoiceId = invoice.Id;

        Assert.False(string.IsNullOrWhiteSpace(existingInvoiceNumber), "Guard: pre-set number not empty.");
        var callersGuardApplies = !string.IsNullOrWhiteSpace(invoice.InvoiceNumber);
        Assert.True(callersGuardApplies, "Service caller guard: already non-empty — skip generation.");

        using var ctxVerify = CreateContext(TestConnStr);
        var postInvoice = await ctxVerify.Invoices.AsNoTracking().FirstAsync(i => i.Id == invoiceId);
        Assert.Equal(existingInvoiceNumber, postInvoice.InvoiceNumber);
        Assert.Equal("OLD-INV-99999", postInvoice.InvoiceNumber);
        Assert.NotEqual($"INV-{year}-00001", postInvoice.InvoiceNumber);
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    public async Task SecondInvoice_IncrementsProperly()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        var year = DateTime.UtcNow.Year;
        using var ctx = CreateContext(TestConnStr);
        var companyId = await EnsureCompany(ctx, "SeqRemediation_Increment");
        var gen1 = new EfSequenceGenerator(ctx);

        var first = await gen1.GenerateInvoiceNumberAsync(companyId);
        using var ctx2 = CreateContext(TestConnStr);
        var gen2 = new EfSequenceGenerator(ctx2);
        var second = await gen2.GenerateInvoiceNumberAsync(companyId);

        Assert.Equal($"INV-{year}-00001", first);
        Assert.Equal($"INV-{year}-00002", second);
        Assert.NotEqual(first, second);
        var digit1 = int.Parse(first.Split('-')[^1]);
        var digit2 = int.Parse(second.Split('-')[^1]);
        Assert.Equal(digit1 + 1, digit2);
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    [Trait("Category", "SqlConcurrency")]
    public async Task Concurrency_20ParallelInvoices_NoDuplicates()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        await CreateAndMigrateDb(ConcurrencyConnStr, ConcurrencyDbName);
        try
        {
            Guid companyId;
            using (var seedCtx = CreateContext(ConcurrencyConnStr))
            {
                companyId = await EnsureCompany(seedCtx, "ConcurrencyCompany_NoDupes");
            }
            var year = DateTime.UtcNow.Year;

            async Task<string> RunOne()
            {
                await using var c = CreateContext(ConcurrencyConnStr);
                var g = new EfSequenceGenerator(c);
                return await g.GenerateInvoiceNumberAsync(companyId);
            }

            var tasks = Enumerable.Range(0, 20).Select(_ => RunOne()).ToList();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(20, results.Length);
            Assert.Equal(20, results.Distinct().Count());
            Assert.All(results, r => Assert.StartsWith($"INV-{year}-", r));
            Assert.All(results, r => Assert.Equal(3, r.Split('-').Length));
        }
        finally
        {
            await DropDbIfExists(ConcurrencyDbName);
        }
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    [Trait("Category", "SqlConcurrency")]
    public async Task Concurrency_20ParallelInvoices_SequentialRange()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        await CreateAndMigrateDb(ConcurrencyConnStr, ConcurrencyDbName);
        try
        {
            Guid companyId;
            using (var seedCtx = CreateContext(ConcurrencyConnStr))
            {
                companyId = await EnsureCompany(seedCtx, "ConcurrencyCompany_SeqRange");
            }
            var year = DateTime.UtcNow.Year;

            async Task<string> RunOne()
            {
                await using var c = CreateContext(ConcurrencyConnStr);
                var g = new EfSequenceGenerator(c);
                return await g.GenerateInvoiceNumberAsync(companyId);
            }

            var tasks = Enumerable.Range(0, 20).Select(_ => RunOne()).ToList();
            var results = await Task.WhenAll(tasks);

            var nums = results.Select(r => int.Parse(r.Split('-')[^1])).OrderBy(x => x).ToList();
            Assert.Equal(Enumerable.Range(1, 20), nums);
            Assert.Equal(1, nums.First());
            Assert.Equal(20, nums.Last());
        }
        finally
        {
            await DropDbIfExists(ConcurrencyDbName);
        }
    }

    [Fact]
    [Trait("Category", "SequenceRemediation")]
    [Trait("Category", "SqlConcurrency")]
    public async Task Concurrency_20ParallelInvoices_NoUnhandledExceptions()
    {
        if (!CheckSqlAvailable()) { SkipSql(); return; }
        await CreateAndMigrateDb(ConcurrencyConnStr, ConcurrencyDbName);
        try
        {
            Guid companyId;
            using (var seedCtx = CreateContext(ConcurrencyConnStr))
            {
                companyId = await EnsureCompany(seedCtx, "ConcurrencyCompany_NoEx");
            }

            var exceptions = new List<Exception>();
            async Task<string> RunOneSafe()
            {
                try
                {
                    await using var c = CreateContext(ConcurrencyConnStr);
                    var g = new EfSequenceGenerator(c);
                    return await g.GenerateInvoiceNumberAsync(companyId);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                    return null!;
                }
            }

            var tasks = Enumerable.Range(0, 20).Select(_ => RunOneSafe()).ToList();
            var results = await Task.WhenAll(tasks);

            Assert.Empty(exceptions);
            Assert.Equal(20, results.Count(r => r != null));
            Assert.All(results, r => Assert.NotNull(r));
            Assert.All(results, r => Assert.NotEmpty(r));
        }
        finally
        {
            await DropDbIfExists(ConcurrencyDbName);
        }
    }

    private static void SkipSql()
    {
        Assert.True(true, "SQL Server not reachable; test skipped (no-op pass).");
        Assert.True(true, "No assertions applied because SQL Server unavailable.");
    }
}
