using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Discount.Dtos;

namespace E_POS.Application.Modules.Tenant.Discount.Contracts;

public interface IPosDiscountService
{
    Task<ApplicationResult<PosDiscountCatalogResponseDto>> ListAvailableAsync(
        TenantRequestContext context, PosDiscountCatalogQueryDto query, CancellationToken cancellationToken);
    Task<ApplicationResult<PosDiscountValidationResponseDto>> ValidateAsync(
        TenantRequestContext context, PosDiscountValidationRequestDto request, CancellationToken cancellationToken);
    Task<ApplicationResult<PosDiscountApplyResponseDto>> ApplyAsync(
        TenantRequestContext context, PosDiscountValidationRequestDto request, CancellationToken cancellationToken);
    Task<ApplicationResult<PosDiscountDecisionResponseDto>> DecideAsync(
        TenantRequestContext context, Guid applicationId, PosDiscountDecisionRequestDto request,
        CancellationToken cancellationToken);
    Task<ApplicationResult<PosDiscountCancelResponseDto>> CancelAsync(
        TenantRequestContext context, Guid applicationId, PosDiscountCancelRequestDto request,
        CancellationToken cancellationToken);
}
