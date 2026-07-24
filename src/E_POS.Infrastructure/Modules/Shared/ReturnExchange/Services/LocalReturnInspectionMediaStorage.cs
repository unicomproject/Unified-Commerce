using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using Microsoft.Extensions.Configuration;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Services;

public sealed class LocalReturnInspectionMediaStorage : IReturnInspectionMediaStorage
{
    private readonly string _rootPath;

    public LocalReturnInspectionMediaStorage(IConfiguration configuration)
    {
        var configured = configuration["ReturnInspectionMedia:StorageRoot"];
        _rootPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "return-inspection-media")
            : configured;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<ReturnInspectionMediaSaveResult> SaveAsync(
        Guid tenantId,
        Guid saleId,
        Guid saleLineId,
        Guid mediaId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var extension = ResolveExtension(contentType);
        var relativeKey = Path.Combine(
            tenantId.ToString("D"),
            saleId.ToString("D"),
            saleLineId.ToString("D"),
            $"{mediaId:D}{extension}");
        var absolutePath = Path.Combine(_rootPath, relativeKey);
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = File.Create(absolutePath);
        await content.CopyToAsync(fileStream, cancellationToken);
        var sizeBytes = fileStream.Length;
        var storageKey = relativeKey.Replace('\\', '/');
        return new ReturnInspectionMediaSaveResult(storageKey, sizeBytes);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private static string ResolveExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg",
        };
}
