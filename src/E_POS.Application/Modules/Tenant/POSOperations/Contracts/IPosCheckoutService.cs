using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosCheckoutService
{
    Task<ApplicationResult<PosCheckoutSummaryResponseDto>> CalculateCartAsync(
        TenantRequestContext context,
        PosCheckoutSummaryRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCheckoutSummaryResponseDto>> GetSummaryAsync(
        TenantRequestContext context,
        PosCheckoutSummaryRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCheckoutStartPaymentResponseDto>> StartPaymentAsync(
        TenantRequestContext context,
        PosCheckoutStartPaymentRequestDto request,
        CancellationToken cancellationToken);
}
