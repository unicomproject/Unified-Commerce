using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public sealed class PlatformAuthRefreshContext
{
    public required PlatformRefreshToken RefreshToken { get; init; }

    public required PlatformAuthSession Session { get; init; }

    public required PlatformUser User { get; init; }
}
