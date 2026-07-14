namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed record TenantAdminProductDeleteOperationResult(
    TenantAdminProductDeleteResponse? Response,
    string? ErrorCode);
