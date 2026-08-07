using LivestockManager.Domain.Entities;

namespace LivestockManager.Domain.Abstractions;

public interface IPdfGenerator
{
    Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Company company);
}
