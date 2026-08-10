namespace E_POS.Application.Modules.Shared.Media.Contracts;

public interface IMediaReadUrlResolver
{
    string? ResolveReadUrl(string? mediaPublicUrl);

    string? ResolveReadUrl(
        string? containerName,
        string? storageKey,
        string? mediaPublicUrl);
}
