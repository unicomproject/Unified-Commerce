namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletImageUploadResponse(
    Guid MediaAssetId,
    string? ImageUrl,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    int WidthPx,
    int HeightPx);
