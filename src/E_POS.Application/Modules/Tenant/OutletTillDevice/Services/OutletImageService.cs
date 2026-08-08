using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using SixLabors.ImageSharp;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public sealed class OutletImageService : IOutletImageService
{
    private const long MaxBytes = 2 * 1024 * 1024;
    private const long MaxPixels = 16_000_000;
    private const string Purpose = "OUTLET_PRIMARY_IMAGE";
    private readonly IOutletImageRepository _repository;
    private readonly IOutletRepository _outletRepository;
    private readonly IMediaObjectStorage _storage;
    private readonly IDateTimeProvider _clock;
    private readonly IOutletAuditLogger _audit;

    public OutletImageService(IOutletImageRepository repository, IOutletRepository outletRepository, IMediaObjectStorage storage, IDateTimeProvider clock, IOutletAuditLogger audit)
    { _repository = repository; _outletRepository = outletRepository; _storage = storage; _clock = clock; _audit = audit; }

    public async Task<ApplicationResult<OutletImageUploadResponse>> UploadAsync(TenantRequestContext context, MediaUploadFile file, CancellationToken cancellationToken)
    {
        var error = await ValidateAccessAsync(context, cancellationToken);
        if (error is not null) return ApplicationResult<OutletImageUploadResponse>.Failure(error);
        if (!_storage.IsConfigured) return ApplicationResult<OutletImageUploadResponse>.Failure(new ApplicationError("outlet.image_storage_unavailable", "Image storage is not configured."));
        if (file.Length is <= 0 or > MaxBytes) return ApplicationResult<OutletImageUploadResponse>.Failure(Invalid("file", "Image file must be between 1 byte and 2 MB."));
        var mime = NormalizeMime(file.ContentType);
        var name = Path.GetFileName(file.FileName ?? string.Empty).Trim();
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!Allowed(mime, extension)) return ApplicationResult<OutletImageUploadResponse>.Failure(Invalid("file", "Only JPG, JPEG and PNG images with a matching MIME type are allowed."));

        await using var memory = new MemoryStream((int)file.Length);
        await file.Content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        if (bytes.Length != file.Length || !MagicMatches(bytes, mime)) return ApplicationResult<OutletImageUploadResponse>.Failure(Invalid("file", "Image signature does not match the declared image type."));
        try
        {
            using var image = Image.Load(bytes);
            if ((long)image.Width * image.Height > MaxPixels) return ApplicationResult<OutletImageUploadResponse>.Failure(Invalid("file", "Image dimensions exceed the 16 MP limit."));
            var id = Guid.NewGuid();
            var now = _clock.UtcNow;
            var safeName = $"outlet-image-{id:N}{extension}";
            var key = $"tenants/{context.TenantId:D}/outlets/staged/{id:N}{extension}";
            memory.Position = 0;
            var uploaded = await _storage.UploadAsync(new MediaObjectUploadRequest(key, memory, mime, new Dictionary<string, string> { ["tenant-id"] = context.TenantId.ToString("D"), ["purpose"] = Purpose }), cancellationToken);
            var asset = MediaAsset.Create(id, context.TenantId, uploaded.ContainerName, uploaded.StorageKey, uploaded.PublicUrl, safeName, mime, extension, bytes.Length, image.Width, image.Height, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), "IMAGE", Purpose, "ACTIVE", context.UserId, now);
            try { await _repository.AddAsync(asset, cancellationToken); await _repository.SaveChangesAsync(cancellationToken); }
            catch { await _storage.DeleteIfExistsAsync(uploaded.ContainerName, uploaded.StorageKey, cancellationToken); throw; }
            _audit.LogImageUploaded(context.TenantId, context.UserId, id);
            return ApplicationResult<OutletImageUploadResponse>.Success(new OutletImageUploadResponse(id, uploaded.PublicUrl, safeName, mime, bytes.Length, image.Width, image.Height));
        }
        catch (UnknownImageFormatException) { return ApplicationResult<OutletImageUploadResponse>.Failure(Invalid("file", "Image data is corrupted or cannot be decoded.")); }
        catch (InvalidImageContentException) { return ApplicationResult<OutletImageUploadResponse>.Failure(Invalid("file", "Image data is corrupted or cannot be decoded.")); }
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var error = await ValidateAccessAsync(context, cancellationToken);
        if (error is not null) return ApplicationResult.Failure(error);
        var asset = await _repository.GetAsync(context.TenantId, mediaAssetId, cancellationToken);
        if (asset is null || asset.AssetType != "IMAGE" || asset.AssetPurpose != Purpose) return ApplicationResult.Failure(new ApplicationError("outlet.image_not_found", "Outlet image was not found."));
        if (await _repository.IsAttachedAsync(context.TenantId, mediaAssetId, cancellationToken)) return ApplicationResult.Failure(new ApplicationError("outlet.image_attached", "An image attached to an outlet cannot be deleted as staged media."));
        if (asset.Status == "DELETED") return ApplicationResult.Success();
        var now = _clock.UtcNow;
        asset.MarkDeletePending(context.UserId, now);
        await _repository.SaveChangesAsync(cancellationToken);
        try { await _storage.DeleteIfExistsAsync(asset.ContainerName, asset.StorageKey, cancellationToken); asset.MarkDeleted(context.UserId, now); await _repository.SaveChangesAsync(cancellationToken); _audit.LogImageRemoved(context.TenantId, context.UserId, mediaAssetId); return ApplicationResult.Success(); }
        catch (Exception ex) when (ex is not OperationCanceledException) { asset.RecordDeletionFailure(ex.Message[..Math.Min(500, ex.Message.Length)], now.AddMinutes(5), context.UserId, now); await _repository.SaveChangesAsync(cancellationToken); return ApplicationResult.Failure(new ApplicationError("outlet.image_delete_unavailable", "Image deletion is pending retry.")); }
    }

    private async Task<ApplicationError?> ValidateAccessAsync(TenantRequestContext context, CancellationToken ct)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty || !context.HasPermission(OutletConstants.ManagePermission)) return new ApplicationError("outlet.permission_denied", "Permission denied for outlet management.");
        if (!await _outletRepository.IsOutletManagementFeatureEnabledAsync(context.TenantId, ct)) return new ApplicationError("outlet.feature_disabled", "Outlet management is not enabled for this tenant.");
        return null;
    }
    private static ApplicationError Invalid(string field, string message) => new("outlet.image_invalid", message, [new ApplicationFieldError(field, message)]);
    private static string NormalizeMime(string? value) => value?.Trim().ToLowerInvariant() switch { "image/jpg" => "image/jpeg", var mime => mime ?? string.Empty };
    private static bool Allowed(string mime, string extension) => mime == "image/jpeg" && extension is ".jpg" or ".jpeg" || mime == "image/png" && extension == ".png";
    private static bool MagicMatches(byte[] bytes, string mime) =>
        mime == "image/jpeg"
            ? bytes.Length > 2 && bytes[0] == 0xFF && bytes[1] == 0xD8
            : mime == "image/png" && bytes.Length > 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
}
