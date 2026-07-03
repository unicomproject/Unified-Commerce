namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record DepartmentSummaryResponse(
    Guid Id,
    string DepartmentCode,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);