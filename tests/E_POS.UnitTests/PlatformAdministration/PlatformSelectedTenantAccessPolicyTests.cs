using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformSelectedTenantAccessPolicyTests
{
    [Fact]
    public async Task AuthorizeMutationAsync_WhenTenantSuspended_ReturnsTenantSuspended()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeBootstrapRepository(new PlatformTenantBootstrapTenantSnapshot(
            tenantId,
            "TEN-001",
            "Tenant One",
            TenantStatusConstants.Suspended,
            "Starter"));

        var policy = new PlatformSelectedTenantAccessPolicy(repository, new AllowAllPermissionChecker());
        var result = await policy.AuthorizeMutationAsync(
            Guid.NewGuid(),
            tenantId,
            PlatformPermissionCodes.TenantsBootstrapOutletsManage,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.bootstrap.tenant_suspended", result.Error.Code);
    }

    [Fact]
    public async Task AuthorizeReadAsync_WhenTenantSuspended_AllowsRead()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeBootstrapRepository(new PlatformTenantBootstrapTenantSnapshot(
            tenantId,
            "TEN-001",
            "Tenant One",
            TenantStatusConstants.Suspended,
            "Starter"));

        var policy = new PlatformSelectedTenantAccessPolicy(repository, new AllowAllPermissionChecker());
        var result = await policy.AuthorizeReadAsync(Guid.NewGuid(), tenantId, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AuthorizeMutationAsync_WhenTenantCancelled_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeBootstrapRepository(new PlatformTenantBootstrapTenantSnapshot(
            tenantId,
            "TEN-001",
            "Tenant One",
            TenantStatusConstants.Cancelled,
            "Starter"));

        var policy = new PlatformSelectedTenantAccessPolicy(repository, new AllowAllPermissionChecker());
        var result = await policy.AuthorizeMutationAsync(
            Guid.NewGuid(),
            tenantId,
            PlatformPermissionCodes.TenantsBootstrapOutletsManage,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.not_found", result.Error.Code);
    }

    [Fact]
    public async Task AuthorizeMutationAsync_WithoutRequiredPermission_ReturnsAccessDenied()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeBootstrapRepository(new PlatformTenantBootstrapTenantSnapshot(
            tenantId,
            "TEN-001",
            "Tenant One",
            TenantStatusConstants.Active,
            "Starter"));

        var policy = new PlatformSelectedTenantAccessPolicy(
            repository,
            new FixedPermissionChecker([PlatformPermissionCodes.TenantsView]));

        var result = await policy.AuthorizeMutationAsync(
            Guid.NewGuid(),
            tenantId,
            PlatformPermissionCodes.TenantsBootstrapOutletsManage,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.bootstrap.access_denied", result.Error.Code);
    }

    private sealed class FakeBootstrapRepository(PlatformTenantBootstrapTenantSnapshot? snapshot)
        : IPlatformTenantBootstrapRepository
    {
        public Task<PlatformTenantBootstrapTenantSnapshot?> GetTenantSnapshotAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);

        public Task<PlatformTenantBootstrapFootprintCounts> GetFootprintCountsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformTenantBootstrapFootprintCounts(0, 0, 0, 1, 0));

        public Task<IReadOnlyList<PlatformTenantBootstrapOutletOptionDto>> ListOutletOptionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlatformTenantBootstrapOutletOptionDto>>([]);

        public Task<IReadOnlyList<PlatformTenantBootstrapRoleOptionDto>> ListRoleOptionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlatformTenantBootstrapRoleOptionDto>>([]);

        public Task<bool> OutletBelongsToTenantAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> RoleBelongsToTenantAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> OutletsBelongToTenantAsync(Guid tenantId, IReadOnlyCollection<Guid> outletIds, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> EmailExistsForTenantAsync(Guid tenantId, string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyDictionary<string, Guid>> GetActivePermissionIdMapByCodesAsync(
            IReadOnlyList<string> permissionCodes,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, Guid>>(new Dictionary<string, Guid>());

        public Task<Guid> CreateCustomRoleAsync(
            Guid tenantId,
            string roleName,
            string? description,
            IReadOnlyList<Guid> permissionIds,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task<Guid?> ResolveCategoryIdByCodeAsync(Guid tenantId, string categoryCode, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);

        public Task<Guid?> ResolveBrandIdByCodeAsync(Guid tenantId, string brandCode, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);

        public Task<Guid?> ResolveOutletIdByCodeAsync(Guid tenantId, string outletCode, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);

        public Task<bool> HasInFlightImportBatchAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportBatch?> GetImportBatchAsync(
            Guid tenantId,
            Guid importId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportBatch?>(null);

        public Task SaveImportBatchAsync(
            Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportBatch batch,
            IReadOnlyList<Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportRow> rows,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UpdateImportBatchAsync(
            Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportBatch batch,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UpdateImportRowsAsync(
            IReadOnlyList<Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportRow> rows,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportRow>> GetImportRowsAsync(
            Guid importId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportRow>>([]);

        public Task<PlatformTenantBootstrapIdempotencyRecordLookup?> TryGetIdempotencyRecordAsync(
            Guid tenantId,
            string operationType,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult<PlatformTenantBootstrapIdempotencyRecordLookup?>(null);

        public Task SaveIdempotencyResponseAsync(
            Guid tenantId,
            string operationType,
            string idempotencyKey,
            string responseJson,
            DateTimeOffset now,
            string? requestHash,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<string?> GetOnlineStoreDefaultsJsonAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        public Task UpsertOnlineStoreDefaultsAsync(
            Guid tenantId,
            string defaultsJson,
            Guid? platformUserId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> HasClickCollectCollectionConfiguredAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class AllowAllPermissionChecker : IPlatformPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class FixedPermissionChecker(IReadOnlyCollection<string> allowed) : IPlatformPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken cancellationToken) =>
            Task.FromResult(allowed.Contains(permissionCode, StringComparer.Ordinal));
    }
}
