using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IReturnPolicyRequestValidator
{
    ApplicationError? ValidateCreate(ReturnPolicyCreateRequest request);
    ApplicationError? ValidateUpdate(ReturnPolicyUpdateRequest request);
}