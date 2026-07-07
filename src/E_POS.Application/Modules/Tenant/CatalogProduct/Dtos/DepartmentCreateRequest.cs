namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record DepartmentCreateRequest(
    string DepartmentCode, 
    string Name, 
    string? Description,
    int SortOrder,
    string Status);
