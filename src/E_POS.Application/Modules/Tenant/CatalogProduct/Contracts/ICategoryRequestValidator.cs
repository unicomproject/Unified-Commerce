using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICategoryRequestValidator
{
    ApplicationError? ValidateCreate(CategoryCreateRequest request);
    ApplicationError? ValidateUpdate(CategoryUpdateRequest request);
}
