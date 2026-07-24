using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.Customer.Contracts;

public interface IPosCustomerRepository
{
    Task<bool> NormalizedPhoneExistsAsync(
        Guid tenantId,
        string normalizedPhone,
        CancellationToken cancellationToken);

    Task<bool> NormalizedPhoneExistsAsync(
        Guid tenantId,
        string normalizedPhone,
        Guid excludingCustomerId,
        CancellationToken cancellationToken);

    Task<bool> NormalizedEmailExistsAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<bool> NormalizedEmailExistsAsync(
        Guid tenantId,
        string normalizedEmail,
        Guid excludingCustomerId,
        CancellationToken cancellationToken);

    Task<bool> AddAsync(CustomerEntity customer, CancellationToken cancellationToken);

    Task<CustomerEntity?> GetTrackedByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<string?> GetCustomerStatusAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(CustomerEntity customer, CancellationToken cancellationToken);

    Task<string?> GetTenantDefaultTimezoneAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<PosCustomerListResponseDto> ListAsync(
        Guid tenantId,
        string? search,
        string? status,
        string? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PosCustomerSummaryResponseDto> GetSummaryAsync(
        Guid tenantId,
        DateTimeOffset monthStartUtc,
        DateTimeOffset monthEndUtc,
        string timeZoneId,
        CancellationToken cancellationToken);

    Task<PosCustomerListItemResponseDto?> GetByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<PosCustomerOrdersResponseDto> GetOrdersAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        CancellationToken cancellationToken);

    Task<bool> TryAssignCustomerToEditableSaleAsync(
        Guid tenantId,
        Guid saleId,
        Guid customerId,
        string? customerNameSnapshot,
        Guid? tillSessionId,
        Guid updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
