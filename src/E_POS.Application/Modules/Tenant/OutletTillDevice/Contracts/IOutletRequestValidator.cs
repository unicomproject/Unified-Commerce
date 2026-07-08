using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface IOutletRequestValidator
{
    ApplicationError? ValidateCreate(OutletCreateRequest request);
    ApplicationError? ValidateUpdate(OutletUpdateRequest request);
}
