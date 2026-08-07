using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Companies;
using LivestockManager.Application.DTOs.Customers;
using LivestockManager.Application.DTOs.Expenses;
using LivestockManager.Application.DTOs.Farms;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Application.DTOs.Livestock;
using LivestockManager.Application.DTOs.Payments;
using LivestockManager.Application.DTOs.Purchases;
using LivestockManager.Application.DTOs.Sales;
using LivestockManager.Application.DTOs.Suppliers;
using LivestockManager.Application.Services.Companies;
using LivestockManager.Application.Services.Customers;
using LivestockManager.Application.Services.Expenses;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Application.Services.Invoices;
using LivestockManager.Application.Services.Livestock;
using LivestockManager.Application.Services.Payments;
using LivestockManager.Application.Services.Purchases;
using LivestockManager.Application.Services.Receipts;
using LivestockManager.Application.Services.Sales;
using LivestockManager.Application.Services.Suppliers;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.UnitTests.Security;

public class CompanyIsolationTests
{
    private static readonly Guid CompanyAIdSeed = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CompanyBIdSeed = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SystemAdminScopeId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static IDateTime CreateFixedClock()
    {
        return new TestIsolationClock { Now = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero) };
    }

    private static ISequenceGenerator CreateSequenceGenerator()
    {
        return new TestIsolationSequenceGenerator();
    }

    private static IPdfGenerator CreatePdfGenerator()
    {
        return new TestIsolationPdfGenerator();
    }

    private static IsolationTestScope CreateTwoCompanyScope()
    {
        var db = CreateInMemoryDb();
        var clock = CreateFixedClock();

        var companyA = new Company("Company A")
        {
            Currency = Currency.USD,
            InvoicePrefix = "INV-A",
            ReceiptPrefix = "RCT-A",
            IsActive = true,
            CreatedAt = clock.Now,
            ModifiedAt = clock.Now
        };
        db.Companies.Add(companyA);

        var companyB = new Company("Company B")
        {
            Currency = Currency.EUR,
            InvoicePrefix = "INV-B",
            ReceiptPrefix = "RCT-B",
            IsActive = true,
            CreatedAt = clock.Now,
            ModifiedAt = clock.Now
        };
        db.Companies.Add(companyB);
        db.SaveChanges();

        var farmA = new Farm(companyA.Id, "Farm A-1", "F-A-1");
        var farmB = new Farm(companyB.Id, "Farm B-1", "F-B-1");
        db.Farms.AddRange(farmA, farmB);
        db.SaveChanges();

        var customerA = new Customer(companyA.Id, "CUST-A-01", "Customer Alpha");
        var customerB = new Customer(companyB.Id, "CUST-B-01", "Customer Beta");
        db.Customers.AddRange(customerA, customerB);
        db.SaveChanges();

        var supplierA = new Supplier(companyA.Id, "Supplier Gamma") { Code = "SUP-A-1" };
        var supplierB = new Supplier(companyB.Id, "Supplier Delta") { Code = "SUP-B-1" };
        db.Suppliers.AddRange(supplierA, supplierB);
        db.SaveChanges();

        var livestockA = new Livestock(
            companyA.Id,
            "AH-A-001",
            LivestockType.PurchasedCastratedRam,
            clock.Now.AddDays(-30),
            45m,
            WeightUnit.Kg,
            600m,
            farmA.Id);
        var livestockB = new Livestock(
            companyB.Id,
            "AH-B-001",
            LivestockType.PurchasedEwe,
            clock.Now.AddDays(-45),
            50m,
            WeightUnit.Kg,
            700m,
            farmB.Id);
        db.Livestock.AddRange(livestockA, livestockB);
        db.SaveChanges();

        var invoiceA = new Invoice(companyA.Id, customerA.Id, clock.Now, clock.Now.AddDays(30));
        invoiceA.Status = InvoiceStatus.Draft;
        invoiceA.GrandTotal = 1000m;
        var invoiceB = new Invoice(companyB.Id, customerB.Id, clock.Now, clock.Now.AddDays(30));
        invoiceB.Status = InvoiceStatus.Draft;
        invoiceB.GrandTotal = 2000m;
        db.Invoices.AddRange(invoiceA, invoiceB);
        db.SaveChanges();

        var saleA = new Sale(companyA.Id, customerA.Id, clock.Now);
        saleA.Status = SaleStatus.Draft;
        saleA.GrandTotal = 1500m;
        var saleB = new Sale(companyB.Id, customerB.Id, clock.Now);
        saleB.Status = SaleStatus.Draft;
        saleB.GrandTotal = 2500m;
        db.Sales.AddRange(saleA, saleB);
        db.SaveChanges();

        var paymentA = new Payment(companyA.Id, customerA.Id, clock.Now, PaymentMethod.Cash, 500m);
        paymentA.InvoiceId = invoiceA.Id;
        var paymentB = new Payment(companyB.Id, customerB.Id, clock.Now, PaymentMethod.BankTransfer, 800m);
        paymentB.InvoiceId = invoiceB.Id;
        db.Payments.AddRange(paymentA, paymentB);
        db.SaveChanges();

        var purchaseA = new Purchase(companyA.Id, supplierA.Id, clock.Now);
        purchaseA.Status = PurchaseStatus.Draft;
        purchaseA.GrandTotal = 3000m;
        var purchaseB = new Purchase(companyB.Id, supplierB.Id, clock.Now);
        purchaseB.Status = PurchaseStatus.Draft;
        purchaseB.GrandTotal = 4000m;
        db.Purchases.AddRange(purchaseA, purchaseB);
        db.SaveChanges();

        var expenseA = new Expense(companyA.Id, ExpenseCategory.Feed, clock.Now, 200m);
        expenseA.FarmId = farmA.Id;
        expenseA.PaymentMethod = PaymentMethod.Cash;
        var expenseB = new Expense(companyB.Id, ExpenseCategory.Veterinary, clock.Now, 300m);
        expenseB.FarmId = farmB.Id;
        expenseB.PaymentMethod = PaymentMethod.Card;
        db.Expenses.AddRange(expenseA, expenseB);
        db.SaveChanges();

        return new IsolationTestScope(
            db,
            clock,
            companyA.Id,
            companyB.Id,
            farmA.Id,
            farmB.Id,
            customerA.Id,
            customerB.Id,
            supplierA.Id,
            supplierB.Id,
            livestockA.Id,
            livestockB.Id,
            invoiceA.Id,
            invoiceB.Id,
            saleA.Id,
            saleB.Id,
            paymentA.Id,
            paymentB.Id,
            purchaseA.Id,
            purchaseB.Id,
            expenseA.Id,
            expenseB.Id);
    }

    [Fact]
    public async Task A_CrossCompanyInvoice_GetById_ThrowsGenericNotFound()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var pdfGen = CreatePdfGenerator();
        var service = new InvoiceService(db, clock, seqGen, pdfGen);

        var ex1 = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetByIdAsync(scope.InvoiceAId, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Invoice not found.", ex1.Message);
        Assert.DoesNotContain(scope.InvoiceAId.ToString(), ex1.Message);

        var ex2 = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetByIdAsync(scope.InvoiceBId, scope.CompanyAId, CancellationToken.None));
        Assert.Equal("Invoice not found.", ex2.Message);
        Assert.DoesNotContain(scope.InvoiceBId.ToString(), ex2.Message);

        var sameCompany = await service.GetByIdAsync(scope.InvoiceAId, scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(sameCompany);
        Assert.Equal(scope.InvoiceAId, sameCompany.Id);
    }

    [Fact]
    public async Task B_CrossCompanyInvoice_Confirm_FailsWithGenericNotFound()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var pdfGen = CreatePdfGenerator();
        var service = new InvoiceService(db, clock, seqGen, pdfGen);

        var confirmDtoA = new InvoiceConfirmDto { InvoiceId = scope.InvoiceAId };
        var exWrongCompany = await Assert.ThrowsAsync<DomainException>(() =>
            service.ConfirmAsync(confirmDtoA, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Invoice not found.", exWrongCompany.Message);

        var invoiceStillDraft = await db.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == scope.InvoiceAId);
        Assert.NotNull(invoiceStillDraft);
        Assert.Equal(InvoiceStatus.Draft, invoiceStillDraft.Status);

        var correctResult = await service.ConfirmAsync(confirmDtoA, scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(correctResult);
        Assert.Equal(InvoiceStatus.Confirmed, correctResult.Status);
    }

    [Fact]
    public async Task C_CrossCompanyInvoice_CancelOrVoid_FailsWithGenericNotFound()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var pdfGen = CreatePdfGenerator();
        var service = new InvoiceService(db, clock, seqGen, pdfGen);

        var exWrongCompany = await Assert.ThrowsAsync<DomainException>(() =>
            service.CancelOrVoidAsync(scope.InvoiceAId, "Wrong company cancel", false, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Invoice not found.", exWrongCompany.Message);
        Assert.DoesNotContain(scope.InvoiceAId.ToString(), exWrongCompany.Message);

        var invoiceUntouched = await db.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == scope.InvoiceAId);
        Assert.NotNull(invoiceUntouched);
        Assert.Equal(InvoiceStatus.Draft, invoiceUntouched.Status);
        Assert.Null(invoiceUntouched.Notes);

        await service.CancelOrVoidAsync(scope.InvoiceAId, "Valid cancel reason", true, scope.CompanyAId, CancellationToken.None);
        var cancelledInvoice = await db.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == scope.InvoiceAId);
        Assert.NotNull(cancelledInvoice);
        Assert.True(cancelledInvoice.Status == InvoiceStatus.Cancelled || cancelledInvoice.Status == InvoiceStatus.Voided);
    }

    [Fact]
    public async Task D_CrossCompanyInvoice_GetPdf_FailsWithGenericNotFound()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var pdfGen = CreatePdfGenerator();
        var service = new InvoiceService(db, clock, seqGen, pdfGen);

        var exWrongCompany = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetPdfAsync(scope.InvoiceAId, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Invoice not found.", exWrongCompany.Message);
        Assert.DoesNotContain(scope.InvoiceAId.ToString(), exWrongCompany.Message);

        var exReverse = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetPdfAsync(scope.InvoiceBId, scope.CompanyAId, CancellationToken.None));
        Assert.Equal("Invoice not found.", exReverse.Message);

        var validBytes = await service.GetPdfAsync(scope.InvoiceAId, scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(validBytes);
        Assert.True(validBytes.Length > 0);
        Assert.Equal(0x25, validBytes[0]);
    }

    [Fact]
    public async Task E_CrossCompanyPayment_Post_FailsBecauseFiltersRejectForeignInvoice()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var pdfGen = CreatePdfGenerator();
        var service = new PaymentService(db, clock, seqGen, pdfGen);

        var maliciousCreate = new PaymentCreateDto
        {
            CompanyId = scope.CompanyAId,
            CustomerId = scope.CustomerAId,
            PaymentDate = clock.Now,
            Method = PaymentMethod.Cash,
            Amount = 999m,
            Reference = "HACK-001",
            Allocations = new List<PaymentAllocationDto>
            {
                new PaymentAllocationDto { InvoiceId = scope.InvoiceAId, Amount = 999m }
            }
        };

        var resultWrong = await Assert.ThrowsAsync<DomainException>(() =>
            service.PostAsync(maliciousCreate, scope.CompanyBId, CancellationToken.None));
        Assert.NotNull(resultWrong);

        var noPaymentsWithHackRef = await db.Payments
            .AnyAsync(p => p.Reference == "HACK-001");
        Assert.False(noPaymentsWithHackRef);

        var countBefore = await db.Payments.CountAsync(p => p.CompanyId == scope.CompanyAId && !p.IsReversed);
        var okDto = new PaymentCreateDto
        {
            CompanyId = scope.CompanyAId,
            CustomerId = scope.CustomerAId,
            PaymentDate = clock.Now,
            Method = PaymentMethod.Cash,
            Amount = 100m,
            Reference = "VALID-001",
            Allocations = new List<PaymentAllocationDto>
            {
                new PaymentAllocationDto { InvoiceId = scope.InvoiceAId, Amount = 100m }
            }
        };
        var okResult = await service.PostAsync(okDto, scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(okResult);
        Assert.Equal(100m, okResult.Amount);
        var countAfter = await db.Payments.CountAsync(p => p.CompanyId == scope.CompanyAId && !p.IsReversed);
        Assert.Equal(countBefore + 1, countAfter);
    }

    [Fact]
    public async Task F_CrossCompanyReceipt_GenerateForPayment_FailsNotFound()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var service = new ReceiptService(db, clock, seqGen);

        var exWrong = await Assert.ThrowsAsync<DomainException>(() =>
            service.GenerateForPaymentAsync(scope.PaymentAId, "Should fail", scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Payment not found.", exWrong.Message);
        Assert.DoesNotContain(scope.PaymentAId.ToString(), exWrong.Message);

        var anyReceiptsForB = await db.Receipts.AnyAsync(r => r.CompanyId == scope.CompanyBId);
        Assert.False(anyReceiptsForB);

        var ok = await service.GenerateForPaymentAsync(scope.PaymentAId, "Valid receipt", scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(ok);
        Assert.Equal(scope.CompanyAId, ok.CompanyId);
        Assert.NotNull(ok.ReceiptNumber);
    }

    [Fact]
    public async Task G_CrossCompanyLivestock_Update_FailsGenericNotFound()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var service = new LivestockService(db, clock, seqGen);

        var editDto = new LivestockEditDto
        {
            AcquisitionDate = clock.Now,
            InitialWeight = 999m,
            WeightUnit = WeightUnit.Kg,
            FarmId = scope.FarmAId,
            Comments = "Hacked"
        };

        var exWrong = await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateAsync(scope.LivestockAId, editDto, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Livestock not found.", exWrong.Message);

        var stillOriginal = await db.Livestock.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == scope.LivestockAId);
        Assert.NotNull(stillOriginal);
        Assert.Equal("AH-A-001", stillOriginal.LivestockId);
        Assert.Equal(45m, stillOriginal.InitialWeight);
        Assert.Equal(600m, stillOriginal.PurchaseAmount);

        var validEdit = new LivestockEditDto
        {
            AcquisitionDate = clock.Now.AddDays(-30),
            InitialWeight = 46m,
            WeightUnit = WeightUnit.Kg,
            FarmId = scope.FarmAId,
            Comments = "Updated record"
        };
        var okResult = await service.UpdateAsync(scope.LivestockAId, validEdit, scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(okResult);
        Assert.Equal(46m, okResult.InitialWeight);
    }

    [Fact]
    public async Task H_CrossCompany_CustomerAndSupplier_GetByIdAndDelete_FailGenericNotFound()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var customerService = new CustomerService(db, clock);
        var supplierService = new SupplierService(db, clock);

        var custEx = await Assert.ThrowsAsync<DomainException>(() =>
            customerService.GetByIdAsync(scope.CustomerAId, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Customer not found.", custEx.Message);
        Assert.DoesNotContain(scope.CustomerAId.ToString(), custEx.Message);

        var supEx = await Assert.ThrowsAsync<DomainException>(() =>
            supplierService.GetByIdAsync(scope.SupplierAId, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Supplier not found.", supEx.Message);
        Assert.DoesNotContain(scope.SupplierAId.ToString(), supEx.Message);

        var custDeleteEx = await Assert.ThrowsAsync<DomainException>(() =>
            customerService.DeleteAsync(scope.CustomerAId, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Customer not found.", custDeleteEx.Message);
        var custStillThere = await db.Customers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == scope.CustomerAId);
        Assert.NotNull(custStillThere);
        Assert.False(custStillThere.IsDeleted);

        var custOk = await customerService.GetByIdAsync(scope.CustomerAId, scope.CompanyAId, CancellationToken.None);
        var supOk = await supplierService.GetByIdAsync(scope.SupplierAId, scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(custOk);
        Assert.NotNull(supOk);
        Assert.Equal(scope.CompanyAId, custOk.CompanyId);
        Assert.Equal(scope.CompanyAId, supOk.CompanyId);
    }

    [Fact]
    public async Task I_MaliciousPayloadCompanyId_IsIgnored_ExplicitParameterWins()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var customerService = new CustomerService(db, clock);
        var farmService = new FarmService(db, clock);

        var poisonedCustomer = new CustomerCreateDto
        {
            CompanyId = scope.CompanyBId,
            CustomerCode = "INJECT-01",
            Name = "Should be Company A"
        };
        var createdCustomer = await customerService.CreateAsync(poisonedCustomer, scope.CompanyAId, CancellationToken.None);
        Assert.Equal(scope.CompanyAId, createdCustomer.CompanyId);
        Assert.NotEqual(scope.CompanyBId, createdCustomer.CompanyId);
        var dbCheckCustomer = await db.Customers.FindAsync(createdCustomer.Id);
        Assert.NotNull(dbCheckCustomer);
        Assert.Equal(scope.CompanyAId, dbCheckCustomer.CompanyId);

        var poisonedFarm = new FarmCreateDto
        {
            CompanyId = scope.CompanyBId,
            Name = "Injected Farm",
            Code = "INJECT-FARM"
        };
        var createdFarm = await farmService.CreateAsync(poisonedFarm, scope.CompanyAId, CancellationToken.None);
        Assert.Equal(scope.CompanyAId, createdFarm.CompanyId);
        Assert.Equal("INJECT-FARM", createdFarm.Code);

        var livestockDto = new LivestockRegisterDto
        {
            CompanyId = scope.CompanyBId,
            LivestockType = LivestockType.PurchasedCastratedRam,
            AcquisitionDate = clock.Now,
            InitialWeight = 55m,
            WeightUnit = WeightUnit.Kg,
            PurchaseAmount = 800m,
            FarmId = scope.FarmAId,
            Comments = "Injected livestock"
        };
        var livestockService = new LivestockService(db, clock, seqGen);
        var createdLivestock = await livestockService.RegisterAsync(livestockDto, scope.CompanyAId, CancellationToken.None);
        Assert.Equal(scope.CompanyAId, createdLivestock.CompanyId);
    }

    [Fact]
    public async Task J_SystemAdministratorScope_Placeholder_PassthroughRespectsFilter()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var customerService = new CustomerService(db, clock);
        var farmService = new FarmService(db, clock);

        var sysAdminScope = SystemAdminScopeId;
        var customerExEmpty = await Assert.ThrowsAsync<DomainException>(() =>
            customerService.GetByIdAsync(scope.CustomerAId, sysAdminScope, CancellationToken.None));
        Assert.Equal("Customer not found.", customerExEmpty.Message);

        var farmExEmpty = await Assert.ThrowsAsync<DomainException>(() =>
            farmService.GetByIdAsync(scope.FarmAId, sysAdminScope, CancellationToken.None));
        Assert.Equal("Farm not found.", farmExEmpty.Message);

        var listFarmsA = await farmService.ListAsync(scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(listFarmsA);
        Assert.All(listFarmsA, f => Assert.Equal(scope.CompanyAId, f.CompanyId));
        Assert.DoesNotContain(listFarmsA, f => f.Id == scope.FarmBId);

        var actualAdminCustomerA = await customerService.GetByIdAsync(scope.CustomerAId, scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(actualAdminCustomerA);
        Assert.Equal(scope.CustomerAId, actualAdminCustomerA.Id);
    }

    [Fact]
    public async Task K_ListMethods_ReturnOnlyScopedCompanyData()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var customerService = new CustomerService(db, clock);
        var supplierService = new SupplierService(db, clock);
        var farmService = new FarmService(db, clock);
        var expenseService = new ExpenseService(db, clock);

        var customersA = await customerService.ListAsync(scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(customersA);
        Assert.Single(customersA);
        Assert.All(customersA, c => Assert.Equal(scope.CompanyAId, c.CompanyId));
        Assert.DoesNotContain(customersA, c => c.Id == scope.CustomerBId);

        var customersB = await customerService.ListAsync(scope.CompanyBId, CancellationToken.None);
        Assert.Single(customersB);
        Assert.All(customersB, c => Assert.Equal(scope.CompanyBId, c.CompanyId));
        Assert.DoesNotContain(customersB, c => c.Id == scope.CustomerAId);

        var suppliersA = await supplierService.ListAsync(scope.CompanyAId, CancellationToken.None);
        Assert.Single(suppliersA);
        Assert.All(suppliersA, s => Assert.Equal("SUP-A-1", s.Code));

        var farmsA = await farmService.ListAsync(scope.CompanyAId, CancellationToken.None);
        Assert.Single(farmsA);
        Assert.All(farmsA, f => Assert.Equal(scope.CompanyAId, f.CompanyId));
        Assert.DoesNotContain(farmsA, f => f.Id == scope.FarmBId);

        var expensesA = await expenseService.ListAsync(scope.CompanyAId, null, null, null, null, null, null, CancellationToken.None);
        Assert.Single(expensesA);
        Assert.All(expensesA, e => Assert.Equal(ExpenseCategory.Feed, e.Category));
    }

    [Fact]
    public async Task L_FarmAndLivestockScopedAccess_HonorsCompanyIdForeignKey()
    {
        using var scope = CreateTwoCompanyScope();
        var db = scope.Db;
        var clock = scope.Clock;
        var seqGen = CreateSequenceGenerator();
        var farmService = new FarmService(db, clock);
        var livestockService = new LivestockService(db, clock, seqGen);
        var companyService = new CompanyService(db, clock);

        var companyBFarmAEx = await Assert.ThrowsAsync<DomainException>(() =>
            farmService.GetByIdAsync(scope.FarmAId, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Farm not found.", companyBFarmAEx.Message);
        Assert.DoesNotContain(scope.FarmAId.ToString(), companyBFarmAEx.Message);

        var companyAFarmBEx = await Assert.ThrowsAsync<DomainException>(() =>
            farmService.GetByIdAsync(scope.FarmBId, scope.CompanyAId, CancellationToken.None));
        Assert.Equal("Farm not found.", companyAFarmBEx.Message);

        var companyBLivestockAEx = await Assert.ThrowsAsync<DomainException>(() =>
            livestockService.GetByIdAsync(scope.LivestockAId, scope.CompanyBId, CancellationToken.None));
        Assert.Equal("Livestock not found.", companyBLivestockAEx.Message);

        var validFarmA = await farmService.GetByIdAsync(scope.FarmAId, scope.CompanyAId, CancellationToken.None);
        Assert.NotNull(validFarmA);
        Assert.Equal(scope.CompanyAId, validFarmA.CompanyId);
        Assert.True(validFarmA.LivestockCount >= 0);

        var validLivestockB = await livestockService.GetByIdAsync(scope.LivestockBId, scope.CompanyBId, CancellationToken.None);
        Assert.NotNull(validLivestockB);
        Assert.Equal(scope.CompanyBId, validLivestockB.CompanyId);
        Assert.Equal(scope.FarmBId, validLivestockB.FarmId);
    }
}

public sealed class IsolationTestScope : IDisposable
{
    public AppDbContext Db { get; }
    public IDateTime Clock { get; }
    public Guid CompanyAId { get; }
    public Guid CompanyBId { get; }
    public Guid FarmAId { get; }
    public Guid FarmBId { get; }
    public Guid CustomerAId { get; }
    public Guid CustomerBId { get; }
    public Guid SupplierAId { get; }
    public Guid SupplierBId { get; }
    public Guid LivestockAId { get; }
    public Guid LivestockBId { get; }
    public Guid InvoiceAId { get; }
    public Guid InvoiceBId { get; }
    public Guid SaleAId { get; }
    public Guid SaleBId { get; }
    public Guid PaymentAId { get; }
    public Guid PaymentBId { get; }
    public Guid PurchaseAId { get; }
    public Guid PurchaseBId { get; }
    public Guid ExpenseAId { get; }
    public Guid ExpenseBId { get; }

    public IsolationTestScope(
        AppDbContext db,
        IDateTime clock,
        Guid companyAId, Guid companyBId,
        Guid farmAId, Guid farmBId,
        Guid customerAId, Guid customerBId,
        Guid supplierAId, Guid supplierBId,
        Guid livestockAId, Guid livestockBId,
        Guid invoiceAId, Guid invoiceBId,
        Guid saleAId, Guid saleBId,
        Guid paymentAId, Guid paymentBId,
        Guid purchaseAId, Guid purchaseBId,
        Guid expenseAId, Guid expenseBId)
    {
        Db = db;
        Clock = clock;
        CompanyAId = companyAId;
        CompanyBId = companyBId;
        FarmAId = farmAId;
        FarmBId = farmBId;
        CustomerAId = customerAId;
        CustomerBId = customerBId;
        SupplierAId = supplierAId;
        SupplierBId = supplierBId;
        LivestockAId = livestockAId;
        LivestockBId = livestockBId;
        InvoiceAId = invoiceAId;
        InvoiceBId = invoiceBId;
        SaleAId = saleAId;
        SaleBId = saleBId;
        PaymentAId = paymentAId;
        PaymentBId = paymentBId;
        PurchaseAId = purchaseAId;
        PurchaseBId = purchaseBId;
        ExpenseAId = expenseAId;
        ExpenseBId = expenseBId;
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}

public class TestIsolationClock : IDateTime
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
}

public class TestIsolationSequenceGenerator : ISequenceGenerator
{
    public Task<string> GenerateLivestockIdAsync(Guid companyId, LivestockType type, CancellationToken ct = default)
    {
        return Task.FromResult($"LS{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant()}");
    }

    public Task<string> GenerateInvoiceNumberAsync(Guid companyId, CancellationToken ct = default)
    {
        return Task.FromResult($"INV-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant()}");
    }

    public Task<string> GenerateReceiptNumberAsync(Guid companyId, CancellationToken ct = default)
    {
        return Task.FromResult($"RCP-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant()}");
    }

    public Task<string> GenerateDocumentNumberAsync(Guid companyId, string prefix, CancellationToken ct = default)
    {
        return Task.FromResult($"{prefix}{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}");
    }
}

public class TestIsolationPdfGenerator : IPdfGenerator
{
    public Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Company company)
    {
        var stub = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x31, 0x2E, 0x34 };
        return Task.FromResult(stub);
    }
}
