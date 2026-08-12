using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public sealed record PlatformTenantBootstrapTenantSnapshot(
    Guid TenantId,
    string TenantCode,
    string TenantName,
    string LifecycleStatus,
    string? PlanName);

public interface IPlatformTenantBootstrapRepository
{
    Task<PlatformTenantBootstrapTenantSnapshot?> GetTenantSnapshotAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<PlatformTenantBootstrapFootprintCounts> GetFootprintCountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<bool> OutletBelongsToTenantAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<bool> RoleBelongsToTenantAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<bool> OutletsBelongToTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken);

    Task<bool> EmailExistsForTenantAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, Guid>> GetActivePermissionIdMapByCodesAsync(
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken);

    Task<Guid> CreateCustomRoleAsync(
        Guid tenantId,
        string roleName,
        string? description,
        IReadOnlyList<Guid> permissionIds,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<Guid?> ResolveCategoryIdByCodeAsync(
        Guid tenantId,
        string categoryCode,
        CancellationToken cancellationToken);

    Task<Guid?> ResolveBrandIdByCodeAsync(
        Guid tenantId,
        string brandCode,
        CancellationToken cancellationToken);

    Task<Guid?> ResolveOutletIdByCodeAsync(
        Guid tenantId,
        string outletCode,
        CancellationToken cancellationToken);

    Task<bool> HasInFlightImportBatchAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<PlatformTenantBootstrapProductImportBatch?> GetImportBatchAsync(
        Guid tenantId,
        Guid importId,
        CancellationToken cancellationToken);

    Task SaveImportBatchAsync(
        PlatformTenantBootstrapProductImportBatch batch,
        IReadOnlyList<PlatformTenantBootstrapProductImportRow> rows,
        CancellationToken cancellationToken);

    Task UpdateImportBatchAsync(
        PlatformTenantBootstrapProductImportBatch batch,
        CancellationToken cancellationToken);

    Task UpdateImportRowsAsync(
        IReadOnlyList<PlatformTenantBootstrapProductImportRow> rows,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformTenantBootstrapProductImportRow>> GetImportRowsAsync(
        Guid importId,
        CancellationToken cancellationToken);

    Task<PlatformTenantBootstrapIdempotencyRecordLookup?> TryGetIdempotencyRecordAsync(
        Guid tenantId,
        string operationType,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task SaveIdempotencyResponseAsync(
        Guid tenantId,
        string operationType,
        string idempotencyKey,
        string responseJson,
        DateTimeOffset now,
        string? requestHash,
        CancellationToken cancellationToken);

    Task<string?> GetOnlineStoreDefaultsJsonAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task UpsertOnlineStoreDefaultsAsync(
        Guid tenantId,
        string defaultsJson,
        Guid? platformUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> HasClickCollectCollectionConfiguredAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
}

public sealed record PlatformTenantBootstrapIdempotencyRecordLookup(
    string ResponseJson,
    string? RequestHash);

public sealed record PlatformTenantBootstrapFootprintCounts(
    int ActiveOutletCount,
    int ActiveTillCount,
    int CustomRoleCount,
    int TenantUserCount,
    int ActiveOrDraftProductCount);

public interface IPlatformTenantBootstrapService
{
    Task<ApplicationResult<PlatformTenantBootstrapSummaryResponse>> GetSummaryAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapOutletResponse>> CreateOutletAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapOutletCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapTillResponse>> CreateTillAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapTillCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapRoleResponse>> CreateRoleAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapRoleCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapUserResponse>> CreateUserAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapUserCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapProductResponse>> CreateProductAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapProductCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ApplicationResult<byte[]>> GetProductImportTemplateAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>> ValidateProductImportAsync(
        Guid tenantId,
        Guid platformUserId,
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>> CommitProductImportAsync(
        Guid tenantId,
        Guid platformUserId,
        Guid importId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ApplicationResult<byte[]>> GetProductImportErrorsCsvAsync(
        Guid tenantId,
        Guid platformUserId,
        Guid importId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>> GetOnlineStoreAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>> UpsertOnlineStoreAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapOnlineStoreUpsertRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
