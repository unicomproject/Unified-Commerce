namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record BrandResponse(
    Guid Id,
    string BrandCode,
    string BrandName,
    string? LogoUrl,
    Guid? LogoMediaAssetId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? Description = null,
    int SortOrder = 0,
    long RowVersion = 1);
