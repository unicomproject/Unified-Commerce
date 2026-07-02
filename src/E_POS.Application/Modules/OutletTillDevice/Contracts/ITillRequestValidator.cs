using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.OutletTillDevice.Contracts;

public interface ITillRequestValidator
{
    ApplicationError? ValidateCreate(TillCreateRequest request);
    ApplicationError? ValidateUpdate(TillUpdateRequest request);
}