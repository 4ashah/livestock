using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using LivestockManager.Application.Common;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Livestock> Livestock => Set<Livestock>();
    public DbSet<LivestockWeight> LivestockWeights => Set<LivestockWeight>();
    public DbSet<LivestockActivity> LivestockActivities => Set<LivestockActivity>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SequenceCounter> SequenceCounters => Set<SequenceCounter>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    #region Phase 2 Entities
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<InvoiceAdditionalCharge> InvoiceAdditionalCharges => Set<InvoiceAdditionalCharge>();
    public DbSet<Document> Documents => Set<Document>();
    #endregion

    async Task<IDbContextTransactionFacade> IAppDbContext.BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new EfDbContextTransactionFacade(transaction);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ConfigureGlobalFilters(builder);
    }

    private void ConfigureGlobalFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");

                var isDeletedProperty = Expression.Property(parameter, nameof(BaseAuditableEntity.IsDeleted));
                var falseConstant = Expression.Constant(false, typeof(bool));
                var isDeletedEqualFalse = Expression.Equal(isDeletedProperty, falseConstant);

                var lambda = Expression.Lambda(isDeletedEqualFalse, parameter);
                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = PrepareAuditEntries();

        foreach (var auditEntry in auditEntries)
        {
            auditEntry.Action = auditEntry.Entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Deleted => "Delete",
                EntityState.Modified => "Update",
                _ => auditEntry.Action
            };
        }

        UpdateTimestamps();
        HandleSoftDeletes();

        var result = await base.SaveChangesAsync(cancellationToken);

        await CreateAuditLogsAsync(auditEntries, cancellationToken);

        return result;
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseAuditableEntity>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;
                entry.Entity.ModifiedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                bool hasRealChange = false;
                foreach (var prop in entry.Properties)
                {
                    if (prop.IsTemporary) continue;
                    var name = prop.Metadata.Name;
                    if (name is "ModifiedAt" or "Version") continue;
                    if (prop.Metadata.IsConcurrencyToken && name == "Version") continue;
                    if (prop.IsModified)
                    {
                        object? orig = prop.OriginalValue;
                        object? curr = prop.CurrentValue;
                        if (!Equals(orig, curr))
                        {
                            hasRealChange = true;
                            break;
                        }
                    }
                }

                if (!hasRealChange)
                {
                    entry.State = EntityState.Unchanged;
                    continue;
                }

                entry.Entity.ModifiedAt = now;
            }
        }
    }

    private void HandleSoftDeletes()
    {
        var entries = ChangeTracker.Entries<BaseAuditableEntity>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.ModifiedAt = now;
            }
        }
    }

    private List<AuditEntry> PrepareAuditEntries()
    {
        var auditEntries = new List<AuditEntry>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                CreatedAt = now,
                UserId = null,
                CompanyId = null
            };

            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }

    private async Task CreateAuditLogsAsync(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        if (auditEntries.Count == 0)
            return;

        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        foreach (var auditEntry in auditEntries)
        {
            if (string.IsNullOrWhiteSpace(auditEntry.Action))
            {
                auditEntry.Action = auditEntry.Entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Deleted => "Delete",
                    EntityState.Modified => "Update",
                    _ => auditEntry.Action
                };
            }

            foreach (var property in auditEntry.Entry.Properties)
            {
                var propertyName = property.Metadata.Name;
                if (propertyName is "Version" or "IsDeleted")
                    continue;

                if (property.Metadata.IsPrimaryKey())
                    continue;

                switch (auditEntry.Entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            if (auditEntry.Action == "Update" && auditEntry.OldValues.Count == 0)
                continue;

            if (string.IsNullOrWhiteSpace(auditEntry.Action))
                continue;

            var auditLog = new AuditLog(auditEntry.Action, auditEntry.CreatedAt)
            {
                EntityType = auditEntry.EntityType,
                EntityId = auditEntry.EntityId,
                OldValuesJson = auditEntry.OldValues.Count > 0 ? JsonSerializer.Serialize(auditEntry.OldValues, options) : null,
                NewValuesJson = auditEntry.NewValues.Count > 0 ? JsonSerializer.Serialize(auditEntry.NewValues, options) : null,
                UserId = auditEntry.UserId,
                CompanyId = auditEntry.CompanyId
            };

            AuditLogs.Add(auditLog);
        }

        if (AuditLogs.Local.Count > 0)
            await base.SaveChangesAsync(cancellationToken);
    }

    private class AuditEntry
    {
        public EntityEntry Entry { get; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
        public Guid? UserId { get; set; }
        public Guid? CompanyId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();

        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }
    }
}

internal class EfDbContextTransactionFacade : IDbContextTransactionFacade
{
    private readonly IDbContextTransaction _transaction;

    public EfDbContextTransactionFacade(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _transaction.DisposeAsync();

    public void Dispose()
        => _transaction.Dispose();
}
