using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public sealed class PlatformAuthRefreshContext
{
    public required PlatformRefreshToken RefreshToken { get; init; }

    public required PlatformAuthSession Session { get; init; }

    public required PlatformUser User { get; init; }
}


