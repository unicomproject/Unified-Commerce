namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletImageResponse(
    Guid MediaAssetId,
    string PublicUrl,
    string MimeType,
    string FileExtension,
    long FileSizeBytes,
    int? WidthPx,
    int? HeightPx);
