namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformPermissionRepository
{
    Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}

