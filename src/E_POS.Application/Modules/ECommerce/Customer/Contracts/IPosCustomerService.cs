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
        string? status,
        string? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCustomerSummaryResponseDto>> GetSummaryAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCustomerListItemResponseDto>> GetByIdAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCustomerOrdersResponseDto>> GetOrdersAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid customerId,
        int page,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCustomerAttachToSaleResponseDto>> AttachToSaleAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid customerId,
        PosCustomerAttachToSaleRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCustomerListItemResponseDto>> UpdateAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid customerId,
        PosCustomerUpdateRequestDto request,
        CancellationToken cancellationToken);
}
