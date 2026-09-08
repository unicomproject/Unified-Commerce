namespace E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;

public sealed record TenantAdminUserProfileImageUploadResponse(
    Guid MediaAssetId,
    string? ImageUrl,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    int WidthPx,
    int HeightPx);
