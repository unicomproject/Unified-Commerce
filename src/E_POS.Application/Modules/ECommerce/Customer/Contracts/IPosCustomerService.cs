using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;

namespace E_POS.Application.Modules.ECommerce.Customer.Contracts;

public interface IPosCustomerService
{
    Task<ApplicationResult<PosCustomerListItemResponseDto>> CreateAsync(
        TenantRequestContext context,
        Guid? deviceId,
        PosCustomerCreateRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCustomerListResponseDto>> ListAsync(
        TenantRequestContext context,
        Guid? deviceId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
