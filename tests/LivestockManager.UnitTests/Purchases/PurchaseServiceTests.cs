using LivestockManager.Application.DTOs.Purchases;
using LivestockManager.Application.Services.Purchases;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.UnitTests.Purchases;

public class PurchaseServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static TestScope CreateTestScope(
        ISequenceGenerator? sequenceGenerator = null,
        IDateTime? dateTime = null)
    {
        var db = CreateInMemoryDb();
        var company = new Company("Test Co") { Currency = Currency.USD, InvoicePrefix = "INV", ReceiptPrefix = "RCT" };
        db.Companies.Add(company);
        var supplier = new Supplier(company.Id, "Test Supplier");
        db.Suppliers.Add(supplier);
        var farm = new Farm(company.Id, "Test Farm", "FARM01");
        db.Farms.Add(farm);
        db.SaveChanges();

        var dt = dateTime ?? new TestDateTime { Now = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero) };
        var seq = sequenceGenerator ?? new TestSequenceGenerator();
        var service = new PurchaseService(db, dt, seq);
        return new TestScope(db, service, company.Id, supplier.Id, farm.Id);
    }

    [Fact]
    public async Task CreateDraftSetsStatusDraft()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId) = scope;

        var dto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)
        };

        var result = await service.CreateDraftAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(PurchaseStatus.Draft, result.Status);
        Assert.Equal(companyId, result.CompanyId);
        Assert.Equal(supplierId, result.SupplierId);
        Assert.Null(result.PurchaseNumber);
    }

    [Fact]
    public async Task CreateDraftWithItems_ComputesCorrectTotals()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId) = scope;

        var dto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Livestock, Description = "Ram 1", Quantity = 1, UnitCost = 200m, DiscountPct = 10m, TaxRate = 15m, UnitWeight = 50m, WeightUnit = WeightUnit.Kg },
                new() { LineNo = 2, ItemType = PurchaseItemType.Feed, Description = "Feed 50kg", Quantity = 3, UnitCost = 50m, DiscountPct = 0, TaxRate = 10m },
                new() { LineNo = 3, ItemType = PurchaseItemType.VeterinarySupplies, Description = "Vaccines", Quantity = 2, UnitCost = 30m, DiscountPct = 0, TaxRate = 0 }
            }
        };

        var result = await service.CreateDraftAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);

        var item1 = result.Items.First(i => i.LineNo == 1);
        var net1 = Math.Round(1 * 200m, 2, MidpointRounding.AwayFromZero);
        var disc1 = Math.Round(net1 * 10 / 100, 2, MidpointRounding.AwayFromZero);
        var afterDisc1 = net1 - disc1;
        var tax1 = Math.Round(afterDisc1 * 15 / 100, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(afterDisc1 + tax1, item1.LineTotal);

        var item2 = result.Items.First(i => i.LineNo == 2);
        var net2 = Math.Round(3 * 50m, 2, MidpointRounding.AwayFromZero);
        var disc2 = Math.Round(net2 * 0 / 100, 2, MidpointRounding.AwayFromZero);
        var afterDisc2 = net2 - disc2;
        var tax2 = Math.Round(afterDisc2 * 10 / 100, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(afterDisc2 + tax2, item2.LineTotal);

        var item3 = result.Items.First(i => i.LineNo == 3);
        var net3 = Math.Round(2 * 30m, 2, MidpointRounding.AwayFromZero);
        var disc3 = Math.Round(net3 * 0 / 100, 2, MidpointRounding.AwayFromZero);
        var afterDisc3 = net3 - disc3;
        var tax3 = Math.Round(afterDisc3 * 0 / 100, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(afterDisc3 + tax3, item3.LineTotal);

        var expectedSubtotal = afterDisc1 + afterDisc2 + afterDisc3;
        var expectedTax = tax1 + tax2 + tax3;
        var expectedGrand = expectedSubtotal + expectedTax;
        Assert.Equal(expectedSubtotal, result.Subtotal);
        Assert.Equal(expectedTax, result.TaxTotal);
        Assert.Equal(expectedGrand, result.GrandTotal);
    }

    [Fact]
    public async Task AddItem_AppendsLineAndRecalculatesTotals()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Item A", Quantity = 1, UnitCost = 100m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        Assert.Equal(100m, created.GrandTotal);

        var newItem = new PurchaseItemCreateDto
        {
            ItemType = PurchaseItemType.Feed,
            Description = "Item B",
            Quantity = 2,
            UnitCost = 50m,
            DiscountPct = 0,
            TaxRate = 0
        };

        var updated = await service.AddItemAsync(created.Id, newItem, CancellationToken.None);

        Assert.Equal(2, updated.Items.Count);
        Assert.Contains(updated.Items, i => i.Description == "Item B");
        Assert.Equal(100m + 100m, updated.GrandTotal);
    }

    [Fact]
    public async Task RemoveItem_RemovesLineAndRecalculatesTotals()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Item A", Quantity = 1, UnitCost = 100m, DiscountPct = 0, TaxRate = 0 },
                new() { LineNo = 2, ItemType = PurchaseItemType.Feed, Description = "Item B", Quantity = 1, UnitCost = 75m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        Assert.Equal(175m, created.GrandTotal);
        var itemToRemove = created.Items.First(i => i.Description == "Item B");

        var updated = await service.RemoveItemAsync(created.Id, itemToRemove.Id, CancellationToken.None);

        Assert.Single(updated.Items);
        Assert.DoesNotContain(updated.Items, i => i.Description == "Item B");
        Assert.Equal(100m, updated.GrandTotal);
    }

    [Fact]
    public async Task PostPurchase_FailsIfNoItems()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);

        var postDto = new PurchasePostDto { Id = created.Id };

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.PostPurchaseAsync(postDto, CancellationToken.None));
        Assert.Contains("at least one item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostPurchase_FailsIfAlreadyPosted()
    {
        var seq = new TestSequenceGenerator();
        var scope = CreateTestScope(sequenceGenerator: seq);
        var (db, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Item", Quantity = 1, UnitCost = 100m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        await service.PostPurchaseAsync(new PurchasePostDto { Id = created.Id }, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.PostPurchaseAsync(new PurchasePostDto { Id = created.Id }, CancellationToken.None));
        Assert.Contains("Only draft", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostPurchase_GeneratesPUR_YYYY_NNNNN()
    {
        var seq = new TestSequenceGenerator();
        seq.SetNextDocumentNumber("PUR:2026", 7);
        var scope = CreateTestScope(sequenceGenerator: seq);
        var (_, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Item", Quantity = 1, UnitCost = 100m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        var posted = await service.PostPurchaseAsync(new PurchasePostDto { Id = created.Id }, CancellationToken.None);

        Assert.Equal("PUR-2026-00007", posted.PurchaseNumber);
        Assert.Equal(PurchaseStatus.Posted, posted.Status);
    }

    [Fact]
    public async Task PostPurchase_CreatesLivestockIntakeIdempotently()
    {
        var seq = new TestSequenceGenerator();
        seq.SetNextDocumentNumber("PUR:2026", 1);
        seq.SetNextLivestockId("LS00001");
        var scope = CreateTestScope(sequenceGenerator: seq);
        var (db, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Livestock, Description = "Ram A", Quantity = 1, UnitCost = 500m, DiscountPct = 0, TaxRate = 0, UnitWeight = 60m, WeightUnit = WeightUnit.Kg }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        Assert.Empty(db.Livestock.ToList());

        var posted = await service.PostPurchaseAsync(new PurchasePostDto { Id = created.Id }, CancellationToken.None);

        var livestockList = db.Livestock.ToList();
        Assert.Single(livestockList);
        var livestockItem = posted.Items.First();
        Assert.NotNull(livestockItem.LivestockId);
        Assert.Equal(livestockList.Single().Id, livestockItem.LivestockId);
        Assert.Equal(LivestockStatus.Active, livestockList.Single().Status);
        Assert.Equal(farmId, livestockList.Single().FarmId);

        seq.SetNextLivestockId("LS00002");
        Assert.Equal(1, db.Livestock.Count());
    }

    [Fact]
    public async Task PostPurchase_DoesNotCreateLivestockForFeedItems()
    {
        var seq = new TestSequenceGenerator();
        seq.SetNextDocumentNumber("PUR:2026", 1);
        var scope = CreateTestScope(sequenceGenerator: seq);
        var (db, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Feed", Quantity = 5, UnitCost = 50m, DiscountPct = 0, TaxRate = 0 },
                new() { LineNo = 2, ItemType = PurchaseItemType.Equipment, Description = "Trough", Quantity = 1, UnitCost = 200m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        var posted = await service.PostPurchaseAsync(new PurchasePostDto { Id = created.Id }, CancellationToken.None);

        Assert.Empty(db.Livestock.ToList());
        Assert.All(posted.Items, i => Assert.Null(i.LivestockId));
    }

    [Fact]
    public async Task PostPurchase_LivestockInitialWeightLinkedIfProvided()
    {
        var seq = new TestSequenceGenerator();
        seq.SetNextDocumentNumber("PUR:2026", 1);
        seq.SetNextLivestockId("LS00099");
        var scope = CreateTestScope(sequenceGenerator: seq);
        var (db, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Livestock, Description = "Ram W", Quantity = 1, UnitCost = 800m, DiscountPct = 0, TaxRate = 0, UnitWeight = 75.5m, WeightUnit = WeightUnit.Kg }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        var posted = await service.PostPurchaseAsync(new PurchasePostDto { Id = created.Id }, CancellationToken.None);

        var livestock = db.Livestock.Single();
        Assert.Equal(75.5m, livestock.InitialWeight);
        Assert.Equal(WeightUnit.Kg, livestock.WeightUnit);

        var weights = db.LivestockWeights.Where(w => w.LivestockId == livestock.Id).ToList();
        Assert.NotEmpty(weights);
        var initialWt = weights.First();
        Assert.Equal(75.5m, initialWt.Weight);
        Assert.Equal(WeightUnit.Kg, initialWt.Unit);
    }

    [Fact]
    public async Task VoidPurchase_FailsIfDraft()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Item", Quantity = 1, UnitCost = 100m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.VoidPurchaseAsync(created.Id, "Duplicate order", CancellationToken.None));
        Assert.Contains("Only posted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VoidPurchase_SetsVoidedGivenReason()
    {
        var seq = new TestSequenceGenerator();
        seq.SetNextDocumentNumber("PUR:2026", 1);
        var scope = CreateTestScope(sequenceGenerator: seq);
        var (_, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Item", Quantity = 1, UnitCost = 100m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        var posted = await service.PostPurchaseAsync(new PurchasePostDto { Id = created.Id }, CancellationToken.None);
        Assert.Equal(PurchaseStatus.Posted, posted.Status);

        var voided = await service.VoidPurchaseAsync(created.Id, "Returned by supplier", CancellationToken.None);

        Assert.Equal(PurchaseStatus.Voided, voided.Status);
        Assert.Contains("Returned by supplier", voided.Notes);
    }

    [Fact]
    public async Task VoidPurchase_FailsIfReasonEmpty()
    {
        var seq = new TestSequenceGenerator();
        seq.SetNextDocumentNumber("PUR:2026", 1);
        var scope = CreateTestScope(sequenceGenerator: seq);
        var (_, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Item", Quantity = 1, UnitCost = 100m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);
        await service.PostPurchaseAsync(new PurchasePostDto { Id = created.Id }, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.VoidPurchaseAsync(created.Id, "   ", CancellationToken.None));
        Assert.Contains("reason is required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListAsync_FiltersByStatusAndSupplier()
    {
        var seq = new TestSequenceGenerator();
        seq.SetNextDocumentNumber("PUR:2026", 1);
        var scope = CreateTestScope(sequenceGenerator: seq);
        var (db, service, companyId, supplierId, farmId) = scope;

        async Task<Guid> Create(string desc, PurchaseStatus finalStatus)
        {
            var dto = new PurchaseCreateDto
            {
                CompanyId = companyId,
                SupplierId = supplierId,
                PurchaseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
                Notes = desc,
                Items = new List<PurchaseItemCreateDto>
                {
                    new() { ItemType = PurchaseItemType.Feed, Description = desc, Quantity = 1, UnitCost = 50m }
                }
            };
            var c = await service.CreateDraftAsync(dto, CancellationToken.None);
            if (finalStatus == PurchaseStatus.Posted)
            {
                seq.SetNextDocumentNumber("PUR:2026", db.Purchases.Count() + 1);
                await service.PostPurchaseAsync(new PurchasePostDto { Id = c.Id }, CancellationToken.None);
            }
            return c.Id;
        }

        var id1 = await Create("Draft A", PurchaseStatus.Draft);
        var id2 = await Create("Posted A", PurchaseStatus.Posted);
        var id3 = await Create("Posted B", PurchaseStatus.Posted);

        var draftList = await service.ListAsync(companyId, null, PurchaseStatus.Draft, null, null, CancellationToken.None);
        Assert.Single(draftList);
        Assert.Equal(id1, draftList.Single().Id);
        Assert.Equal("Test Supplier", draftList.Single().SupplierName);

        var postedList = await service.ListAsync(companyId, null, PurchaseStatus.Posted, null, null, CancellationToken.None);
        Assert.Equal(2, postedList.Count);

        var allList = await service.ListAsync(companyId, null, null, null, null, CancellationToken.None);
        Assert.Equal(3, allList.Count);
    }

    [Fact]
    public async Task GetById_ReturnsDetailWithAllItems()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId) = scope;

        var createDto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            PurchaseDate = new DateTimeOffset(2026, 2, 20, 0, 0, 0, TimeSpan.Zero),
            Currency = Currency.EUR,
            Notes = "Monthly order",
            Items = new List<PurchaseItemCreateDto>
            {
                new() { LineNo = 1, ItemType = PurchaseItemType.Feed, Description = "Hay", Quantity = 10, UnitCost = 15m, DiscountPct = 0, TaxRate = 5m },
                new() { LineNo = 2, ItemType = PurchaseItemType.Medication, Description = "Dewormer", Quantity = 2, UnitCost = 30m, DiscountPct = 0, TaxRate = 0 }
            }
        };
        var created = await service.CreateDraftAsync(createDto, CancellationToken.None);

        var fetched = await service.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(Currency.EUR, fetched.Currency);
        Assert.Equal("Monthly order", fetched.Notes);
        Assert.Equal(2, fetched.Items.Count);
        Assert.Contains(fetched.Items, i => i.Description == "Hay");
        Assert.Contains(fetched.Items, i => i.Description == "Dewormer");
    }
}

public sealed class TestScope : IDisposable
{
    public AppDbContext Db { get; }
    public PurchaseService Service { get; }
    public Guid CompanyId { get; }
    public Guid SupplierId { get; }
    public Guid? FarmId { get; }

    public TestScope(AppDbContext db, PurchaseService service, Guid companyId, Guid supplierId, Guid? farmId)
    {
        Db = db;
        Service = service;
        CompanyId = companyId;
        SupplierId = supplierId;
        FarmId = farmId;
    }

    public void Deconstruct(out AppDbContext db, out PurchaseService service, out Guid companyId, out Guid supplierId, out Guid? farmId)
    {
        db = Db;
        service = Service;
        companyId = CompanyId;
        supplierId = SupplierId;
        farmId = FarmId;
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}

public class TestDateTime : IDateTime
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
}

public class TestSequenceGenerator : ISequenceGenerator
{
    private readonly Dictionary<string, long> _documentCounters = new();
    private readonly Queue<string> _livestockIds = new();

    public void SetNextDocumentNumber(string prefix, long number)
    {
        _documentCounters[prefix] = number;
    }

    public void SetNextLivestockId(string nextId)
    {
        _livestockIds.Enqueue(nextId);
    }

    public Task<string> GenerateLivestockIdAsync(Guid companyId, LivestockType type)
    {
        if (_livestockIds.Count > 0)
            return Task.FromResult(_livestockIds.Dequeue());
        return Task.FromResult($"LS{1:D5}");
    }

    public Task<string> GenerateInvoiceNumberAsync(Guid companyId)
    {
        return Task.FromResult($"INV-{DateTime.UtcNow.Year}-00001");
    }

    public Task<string> GenerateReceiptNumberAsync(Guid companyId)
    {
        return Task.FromResult($"RCT-{DateTime.UtcNow.Year}-00001");
    }

    public Task<string> GenerateDocumentNumberAsync(Guid companyId, string prefix)
    {
        if (_documentCounters.TryGetValue(prefix, out var n))
        {
            return Task.FromResult($"{prefix}{n}");
        }
        return Task.FromResult($"{prefix}1");
    }
}
