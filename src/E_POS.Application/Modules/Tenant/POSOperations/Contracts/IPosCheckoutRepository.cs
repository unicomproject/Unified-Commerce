using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosCheckoutRepository
{
    Task<PosCheckoutCalculationResult> CalculateSummaryAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        PosCheckoutSummaryRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosCheckoutStartPaymentResult> StartPaymentAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        PosCheckoutStartPaymentRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosCheckoutCalculationResult(
    string? ErrorCode,
    PosCheckoutSummaryResponseDto? Summary)
{
    public bool IsSuccess => ErrorCode is null && Summary is not null;
}

public sealed record PosCheckoutStartPaymentResult(
    string? ErrorCode,
    PosCheckoutStartPaymentResponseDto? Payment)
{
    public bool IsSuccess => ErrorCode is null && Payment is not null;
}
