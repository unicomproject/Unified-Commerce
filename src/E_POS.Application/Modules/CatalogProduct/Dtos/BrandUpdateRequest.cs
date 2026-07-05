namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record BrandUpdateRequest(
    string BrandCode, 
    string Name, 
    string? BrandSlug,
    string? Description,
    string? LogoUrl,
    string Status);