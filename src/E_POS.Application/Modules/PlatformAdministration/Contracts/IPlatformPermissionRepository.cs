namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformPermissionRepository
{
    Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}
