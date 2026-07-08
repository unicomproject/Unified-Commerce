using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IReturnPolicyTemplateRequestValidator
{
    ApplicationError? ValidateCreate(ReturnPolicyTemplateCreateRequest request);
    ApplicationError? ValidateUpdate(ReturnPolicyTemplateUpdateRequest request);
}
