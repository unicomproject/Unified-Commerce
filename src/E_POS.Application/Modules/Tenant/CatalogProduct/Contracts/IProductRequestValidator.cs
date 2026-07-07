using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IProductRequestValidator
{
    ApplicationError? ValidateCreate(ProductCreateRequest request);
    ApplicationError? ValidateUpdate(ProductUpdateRequest request);
}

