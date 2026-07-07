using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IBrandRequestValidator
{
    ApplicationError? ValidateCreate(BrandCreateRequest request);
    ApplicationError? ValidateUpdate(BrandUpdateRequest request);
}
