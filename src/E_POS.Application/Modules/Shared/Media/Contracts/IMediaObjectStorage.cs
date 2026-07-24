namespace E_POS.Application.Modules.Shared.Media.Contracts;

public interface IMediaObjectStorage
{
    bool IsConfigured { get; }

    Task<MediaObjectUploadResult> UploadAsync(
        MediaObjectUploadRequest request,
        CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(
        string containerName,
        string storageKey,
        CancellationToken cancellationToken);
}

public sealed record MediaObjectUploadRequest(
    string StorageKey,
    Stream Content,
    string ContentType,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record MediaObjectUploadResult(
    string ContainerName,
    string StorageKey,
    string? PublicUrl);
