using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IDepartmentRequestValidator
{
    ApplicationError? ValidateCreate(DepartmentCreateRequest request);
    ApplicationError? ValidateUpdate(DepartmentUpdateRequest request);
}