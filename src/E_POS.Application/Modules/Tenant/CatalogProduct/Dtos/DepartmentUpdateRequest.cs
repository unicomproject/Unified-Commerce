namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record DepartmentUpdateRequest(
    string DepartmentCode, 
    string Name, 
    string? Description,
    int SortOrder,
    string Status);
