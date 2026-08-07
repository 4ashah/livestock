using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Domain.Abstractions;

public interface IProtectedDocumentStorage
{
    Task<(Guid DocumentId, string SafeInternalPath, long SizeBytes)> StoreAsync(
        Guid companyId, string sanitizedDisplayName, Stream content, DocumentType allowedType,
        long maxSizeBytes, string[] allowedExtensions, Guid? uploadedByUserId, CancellationToken ct);

    Task<(Stream FileStream, Document Metadata)> DownloadAsync(
        Guid documentId, Guid companyId, CancellationToken ct);
}
