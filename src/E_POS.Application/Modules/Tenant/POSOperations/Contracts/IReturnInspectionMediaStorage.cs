namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IReturnInspectionMediaStorage
{
    Task<ReturnInspectionMediaSaveResult> SaveAsync(
        Guid tenantId,
        Guid saleId,
        Guid saleLineId,
        Guid mediaId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed record ReturnInspectionMediaSaveResult(
    string StorageKey,
    long SizeBytes);
