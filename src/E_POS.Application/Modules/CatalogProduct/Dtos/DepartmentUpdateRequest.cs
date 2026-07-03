namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record DepartmentUpdateRequest(string DepartmentCode, string Name, string Status);