using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Application.Common;

public interface IAppDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<Farm> Farms { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Livestock> Livestock { get; }
    DbSet<LivestockWeight> LivestockWeights { get; }
    DbSet<LivestockActivity> LivestockActivities { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<SequenceCounter> SequenceCounters { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseItem> PurchaseItems { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<InvoiceAdditionalCharge> InvoiceAdditionalCharges { get; }
    DbSet<LivestockLoss> LivestockLosses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransactionFacade> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
