using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantAdminProductRequestValidator
{
    ApplicationError? ValidateCreate(TenantAdminProductCreateRequest request);

    ApplicationError? ValidateUpdate(TenantAdminProductCreateRequest request);

    ApplicationError? ValidateStatusUpdate(TenantAdminProductStatusUpdateRequest request);
}
