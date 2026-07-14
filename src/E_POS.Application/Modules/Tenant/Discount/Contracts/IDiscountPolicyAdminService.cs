using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Discount.Dtos;

namespace E_POS.Application.Modules.Tenant.Discount.Contracts;

public interface IDiscountPolicyAdminService
{
    Task<ApplicationResult<IReadOnlyList<DiscountPolicyAdminResponseDto>>> ListAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<DiscountPolicyAdminResponseDto>> GetAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
    Task<ApplicationResult<DiscountPolicyAdminResponseDto>> CreateAsync(TenantRequestContext context, DiscountPolicyAdminRequestDto request, CancellationToken cancellationToken);
    Task<ApplicationResult<DiscountPolicyAdminResponseDto>> UpdateAsync(TenantRequestContext context, Guid id, DiscountPolicyAdminRequestDto request, CancellationToken cancellationToken);
    Task<ApplicationResult<DiscountPolicyAdminResponseDto>> SetActiveAsync(TenantRequestContext context, Guid id, bool active, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> DeleteAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
}

public interface IDiscountPolicyAdminRepository
{
    Task<(string? Error, IReadOnlyList<DiscountPolicyAdminResponseDto> Items)> ListAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<(string? Error, DiscountPolicyAdminResponseDto? Item)> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<(string? Error, DiscountPolicyAdminResponseDto? Item)> SaveAsync(Guid tenantId, Guid userId, Guid? id, DiscountPolicyAdminRequestDto request, DateTimeOffset now, CancellationToken cancellationToken);
    Task<(string? Error, DiscountPolicyAdminResponseDto? Item)> SetActiveAsync(Guid tenantId, Guid userId, Guid id, bool active, DateTimeOffset now, CancellationToken cancellationToken);
    Task<string?> DeleteAsync(Guid tenantId, Guid userId, Guid id, DateTimeOffset now, CancellationToken cancellationToken);
}
