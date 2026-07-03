using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IReturnPolicyTemplateRequestValidator
{
    ApplicationError? ValidateCreate(ReturnPolicyTemplateCreateRequest request);
    ApplicationError? ValidateUpdate(ReturnPolicyTemplateUpdateRequest request);
}