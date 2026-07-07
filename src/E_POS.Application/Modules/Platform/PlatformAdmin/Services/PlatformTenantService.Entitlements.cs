using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed partial class PlatformTenantService
{
    public async Task<ApplicationResult<PlatformTenantEntitlementOptionsResponse>> GetEntitlementOptionsAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
                platformUserId,
                PlatformPermissionCodes.TenantsEntitlementsUpdate,
                cancellationToken))
        {
            return ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Failure(AccessDenied);
        }

        var tenant = await _repository.GetTenantEntityByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Failure(NotFound);
        }

        var subscription = await _repository.GetCurrentTenantSubscriptionEntityAsync(tenantId, cancellationToken);
        if (subscription is null)
        {
            return ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Failure(
                ValidationFailed with { Message = "Tenant subscription was not found." });
        }

        var options = await _repository.GetEntitlementOptionsAsync(tenantId, cancellationToken);
        if (options is null)
        {
            return ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Failure(NotFound);
        }

        return ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Success(options);
    }
}


