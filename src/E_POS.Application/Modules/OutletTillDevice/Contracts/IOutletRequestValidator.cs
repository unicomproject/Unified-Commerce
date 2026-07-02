using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.OutletTillDevice.Contracts;

public interface IOutletRequestValidator
{
    ApplicationError? ValidateCreate(OutletCreateRequest request);
    ApplicationError? ValidateUpdate(OutletUpdateRequest request);
}