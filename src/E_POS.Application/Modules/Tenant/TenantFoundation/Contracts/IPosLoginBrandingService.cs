using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface IPosLoginBrandingService
{
    Task<ApplicationResult<PublicPosLoginBrandingResponse>> GetPublicAsync(string tenantSlug, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantAdminPosLoginBrandingResponse>> GetAdminAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantAdminPosLoginBrandingResponse>> UpdateAdminAsync(TenantRequestContext context, UpdatePosLoginBrandingRequest request, CancellationToken cancellationToken);
}
