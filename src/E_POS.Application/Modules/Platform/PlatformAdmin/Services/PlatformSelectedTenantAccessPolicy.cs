using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformSelectedTenantAccessPolicy
{
    public static readonly ApplicationError AccessDenied = new(
        "platform_tenants.bootstrap.access_denied",
        "Selected-tenant bootstrap access denied.");

    public static readonly ApplicationError NotFound = new(
        "platform_tenants.not_found",
        "Platform tenant not found.");

    public static readonly ApplicationError TenantSuspended = new(
        "platform_tenants.bootstrap.tenant_suspended",
        "Tenant is suspended. Bootstrap mutations are blocked.");

    public static readonly ApplicationError NotEntitled = new(
        "platform_tenants.bootstrap.not_entitled",
        "Tenant is not entitled for this bootstrap module.");

    private readonly IPlatformTenantBootstrapRepository _repository;
    private readonly IPlatformPermissionChecker _permissionChecker;

    public PlatformSelectedTenantAccessPolicy(
        IPlatformTenantBootstrapRepository repository,
        IPlatformPermissionChecker permissionChecker)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
    }

    public async Task<ApplicationResult<PlatformSelectedTenantAccessContext>> AuthorizeReadAsync(
        Guid platformUserId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await AuthorizeInternalAsync(
            platformUserId,
            tenantId,
            PlatformPermissionCodes.TenantsBootstrapAccess,
            allowSuspended: true,
            cancellationToken);
    }

    public async Task<ApplicationResult<PlatformSelectedTenantAccessContext>> AuthorizeMutationAsync(
        Guid platformUserId,
        Guid tenantId,
        string requiredPermission,
        CancellationToken cancellationToken)
    {
        if (!await _permissionChecker.HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsView, cancellationToken))
        {
            return ApplicationResult<PlatformSelectedTenantAccessContext>.Failure(AccessDenied);
        }

        return await AuthorizeInternalAsync(
            platformUserId,
            tenantId,
            requiredPermission,
            allowSuspended: false,
            cancellationToken);
    }

    private async Task<ApplicationResult<PlatformSelectedTenantAccessContext>> AuthorizeInternalAsync(
        Guid platformUserId,
        Guid tenantId,
        string requiredPermission,
        bool allowSuspended,
        CancellationToken cancellationToken)
    {
        if (!await _permissionChecker.HasPermissionAsync(platformUserId, requiredPermission, cancellationToken))
        {
            return ApplicationResult<PlatformSelectedTenantAccessContext>.Failure(AccessDenied);
        }

        var snapshot = await _repository.GetTenantSnapshotAsync(tenantId, cancellationToken);
        if (snapshot is null)
        {
            return ApplicationResult<PlatformSelectedTenantAccessContext>.Failure(NotFound);
        }

        if (string.Equals(snapshot.LifecycleStatus, TenantStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PlatformSelectedTenantAccessContext>.Failure(NotFound);
        }

        if (!allowSuspended &&
            string.Equals(snapshot.LifecycleStatus, TenantStatusConstants.Suspended, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PlatformSelectedTenantAccessContext>.Failure(TenantSuspended);
        }

        return ApplicationResult<PlatformSelectedTenantAccessContext>.Success(
            new PlatformSelectedTenantAccessContext(snapshot, platformUserId));
    }
}

public sealed record PlatformSelectedTenantAccessContext(
    PlatformTenantBootstrapTenantSnapshot Snapshot,
    Guid PlatformUserId);
