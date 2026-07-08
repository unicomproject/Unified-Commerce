namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record DepartmentSummaryResponse(
    Guid Id,
    string DepartmentCode,
    string DepartmentName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
