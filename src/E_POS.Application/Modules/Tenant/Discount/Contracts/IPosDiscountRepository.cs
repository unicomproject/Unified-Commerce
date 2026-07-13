using E_POS.Application.Modules.Tenant.Discount.Dtos;

namespace E_POS.Application.Modules.Tenant.Discount.Contracts;

public interface IPosDiscountRepository
{
    Task<PosDiscountCatalogRepositoryResult> ListAvailableAsync(
        Guid tenantId, Guid tenantUserId, PosDiscountCatalogQueryDto query, DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosDiscountContextRepositoryResult> ResolveContextAsync(
        Guid tenantId, Guid tenantUserId, Guid deviceId, Guid discountPolicyId,
        PosDiscountApplicabilityContext applicability, DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosDiscountContextRepositoryResult> ResolveManualContextAsync(
        Guid tenantId, Guid tenantUserId, Guid deviceId, string calculationMethod,
        string requestedScope, DateTimeOffset now, CancellationToken cancellationToken);

    Task<PosDiscountApplicationRepositoryResult> CreateApplicationAsync(
        PosDiscountApplicationCommand command, CancellationToken cancellationToken);

    Task<PosDiscountDecisionRepositoryResult> DecideAsync(
        Guid tenantId, Guid managerUserId, Guid applicationId, string decision,
        string? note, DateTimeOffset now, CancellationToken cancellationToken);

    Task<PosDiscountCancelRepositoryResult> CancelAsync(
        Guid tenantId, Guid tenantUserId, Guid applicationId, Guid deviceId,
        string? reason, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed record PosDiscountApplicabilityContext(
    string Scope,
    Guid? VariantId,
    IReadOnlyCollection<Guid> VariantIds,
    Guid? CustomerId,
    decimal Quantity,
    decimal CartSubtotal,
    string? CurrencyCode);

public sealed record PosDiscountCatalogRepositoryResult(
    string? ErrorCode,
    PosDiscountCatalogResponseDto? Catalog)
{
    public bool IsSuccess => ErrorCode is null && Catalog is not null;
}

public sealed record PosDiscountPolicySnapshot(
    Guid Id,
    Guid DiscountTypeId,
    string Code,
    string Name,
    string? Description,
    string Scope,
    string CalculationMethod,
    decimal PredefinedValue,
    decimal AbsoluteValueLimit,
    string? CurrencyCode,
    decimal? MaxDiscountAmount,
    decimal? MinOrderAmount,
    decimal? MinQuantity,
    bool RequiresManagerApproval,
    bool IsStackable,
    string? StackingGroupCode,
    int Priority,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);

public sealed record PosDiscountContextRepositoryResult(
    string? ErrorCode,
    Guid OutletId,
    Guid TillId,
    Guid TillSessionId,
    PosDiscountAuthorityDto? Authority,
    PosDiscountPolicySnapshot? Policy)
{
    public bool IsSuccess => ErrorCode is null && Authority is not null && Policy is not null;
}

public sealed record PosDiscountApplicationCommand(
    Guid ApplicationId,
    Guid TenantId,
    Guid DiscountPolicyId,
    Guid DiscountTypeId,
    Guid OutletId,
    Guid TillId,
    Guid TillSessionId,
    Guid DeviceId,
    Guid RequestedByTenantUserId,
    Guid? CustomerId,
    Guid? TargetVariantId,
    string IdempotencyKey,
    string DiscountSource,
    string DiscountScope,
    string PolicyCode,
    string PolicyName,
    string CalculationMethod,
    decimal RequestedValue,
    decimal CashierLimit,
    decimal AbsoluteLimit,
    decimal CartSubtotal,
    decimal EligibleSubtotal,
    decimal DiscountAmount,
    decimal TotalAfterDiscount,
    string CurrencyCode,
    string CartSnapshotJson,
    string CartHash,
    string? Reason,
    bool RequiresManagerApproval,
    bool IsStackable,
    string? StackingGroupCode,
    DateTimeOffset ExpiresAt,
    DateTimeOffset Now);

public sealed record PosDiscountApplicationRepositoryResult(
    string? ErrorCode,
    Guid ApplicationId,
    string Status,
    DateTimeOffset ExpiresAt,
    bool WasExisting)
{
    public bool IsSuccess => ErrorCode is null;
}

public sealed record PosDiscountDecisionRepositoryResult(
    string? ErrorCode,
    PosDiscountDecisionResponseDto? Decision)
{
    public bool IsSuccess => ErrorCode is null && Decision is not null;
}

public sealed record PosDiscountCancelRepositoryResult(
    string? ErrorCode,
    PosDiscountCancelResponseDto? Cancellation)
{
    public bool IsSuccess => ErrorCode is null && Cancellation is not null;
}
