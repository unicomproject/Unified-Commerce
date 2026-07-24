namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record ProductImageUploadRequest(
    Guid? ProductVariantId,
    Guid? SalesChannelId,
    string? AltText,
    string? ImagePurpose,
    int? SortOrder,
    bool? IsPrimaryImage);
