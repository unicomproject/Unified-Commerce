namespace E_POS.Application.Modules.Shared.Media.Dtos;

public sealed record MediaUploadFile(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);

public sealed record MediaAssetUploadResponse(
    Guid MediaAssetId,
    Guid? ProductImageId,
    Guid? ProductId,
    Guid? ProductVariantId,
    Guid? CategoryId,
    Guid? BrandId,
    string ContainerName,
    string StorageKey,
    string? PublicUrl,
    string? ImageUrl,
    string? LogoUrl,
    string OriginalFileName,
    string MimeType,
    string FileExtension,
    long FileSizeBytes,
    int? WidthPx,
    int? HeightPx,
    string ChecksumHash);
