using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface ICollectionRequestValidator
{
    ApplicationError? ValidateCreate(CollectionCreateRequest request);
    ApplicationError? ValidateUpdate(CollectionUpdateRequest request);
}