namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record BrandUpdateRequest(
    string BrandCode, 
    string Name, 
    string? BrandSlug,
    string? Description,
    string? LogoUrl,
    string Status);
