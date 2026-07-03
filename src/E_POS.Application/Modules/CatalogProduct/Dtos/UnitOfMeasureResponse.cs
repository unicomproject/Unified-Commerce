namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record UnitOfMeasureResponse(
    Guid Id,
    Guid? TenantId,
    string UomCode,
    string Name,
    decimal? ConversionFactor,
    bool IsGlobal,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);