using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IProductRequestValidator
{
    ApplicationError? ValidateCreate(ProductCreateRequest request);
    ApplicationError? ValidateUpdate(ProductUpdateRequest request);
}
