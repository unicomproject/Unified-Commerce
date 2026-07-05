namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record DepartmentResponse(
    Guid Id,
    string DepartmentCode,
    string DepartmentName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);