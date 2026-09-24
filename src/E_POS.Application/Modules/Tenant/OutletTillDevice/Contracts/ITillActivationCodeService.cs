using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public sealed record IssuedTillActivationCode(string ActivationCode, DateTimeOffset ExpiresAt);

public interface ITillActivationCodeService
{
    Task<IssuedTillActivationCode> IssueAsync(TenantRequestContext actor, Guid tillId, CancellationToken ct);
}
