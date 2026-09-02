using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using SixLabors.ImageSharp;

namespace E_POS.Application.Modules.Tenant.AccessControl.Services;

public sealed class TenantAdminUserProfileImageService : ITenantAdminUserProfileImageService
{
    public const string AssetPurpose = "TENANT_USER_PROFILE_IMAGE";
    private const long MaxBytes = 2 * 1024 * 1024;
    private const long MaxPixels = 16_000_000;

    private readonly ITenantAdminUserProfileImageRepository _repository;
    private readonly IMediaObjectStorage _storage;
    private readonly IDateTimeProvider _clock;
    private readonly IMediaReadUrlResolver _mediaReadUrlResolver;

    public TenantAdminUserProfileImageService(
        ITenantAdminUserProfileImageRepository repository,
        IMediaObjectStorage storage,
        IDateTimeProvider clock,
        IMediaReadUrlResolver mediaReadUrlResolver)
    {
        _repository = repository;
        _storage = storage;
        _clock = clock;
        _mediaReadUrlResolver = mediaReadUrlResolver;
    }

    public async Task<ApplicationResult<TenantAdminUserProfileImageUploadResponse>> UploadAsync(
        TenantRequestContext context,
        MediaUploadFile file,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Failure(accessError);
        }

        if (!_storage.IsConfigured)
        {
            return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Failure(
                new ApplicationError("user.profile_image_storage_unavailable", "Image storage is not configured."));
        }

        if (file.Length is <= 0 or > MaxBytes)
        {
            return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Failure(
                Invalid("Image file must be between 1 byte and 2 MB."));
        }

        var mimeType = NormalizeMime(file.ContentType);
        var originalFileName = Path.GetFileName(file.FileName ?? string.Empty).Trim();
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!IsAllowed(mimeType, extension))
        {
            return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Failure(
                Invalid("Only JPG, JPEG and PNG images with a matching MIME type are allowed."));
        }

        await using var memory = new MemoryStream((int)file.Length);
        await file.Content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        if (bytes.Length != file.Length || !MagicMatches(bytes, mimeType))
        {
            return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Failure(
                Invalid("Image signature does not match the declared image type."));
        }

        try
        {
            using var image = Image.Load(bytes);
            if ((long)image.Width * image.Height > MaxPixels)
            {
                return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Failure(
                    Invalid("Image dimensions exceed the 16 MP limit."));
            }

            var mediaAssetId = Guid.NewGuid();
            var now = _clock.UtcNow;
            var safeName = $"user-profile-{mediaAssetId:N}{extension}";
            var storageKey = $"tenants/{context.TenantId:D}/users/staged/{mediaAssetId:N}{extension}";
            memory.Position = 0;
            var uploaded = await _storage.UploadAsync(
                new MediaObjectUploadRequest(
                    storageKey,
                    memory,
                    mimeType,
                    new Dictionary<string, string>
                    {
                        ["tenant-id"] = context.TenantId.ToString("D"),
                        ["purpose"] = AssetPurpose
                    }),
                cancellationToken);

            var asset = MediaAsset.Create(
                mediaAssetId,
                context.TenantId,
                uploaded.ContainerName,
                uploaded.StorageKey,
                uploaded.PublicUrl,
                safeName,
                mimeType,
                extension,
                bytes.Length,
                image.Width,
                image.Height,
                Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                "IMAGE",
                AssetPurpose,
                "ACTIVE",
                context.UserId,
                now);

            try
            {
                await _repository.AddAsync(asset, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _storage.DeleteIfExistsAsync(uploaded.ContainerName, uploaded.StorageKey, cancellationToken);
                throw;
            }

            var imageUrl = _mediaReadUrlResolver.ResolveReadUrl(
                uploaded.ContainerName,
                uploaded.StorageKey,
                uploaded.PublicUrl);
            return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Success(
                new TenantAdminUserProfileImageUploadResponse(
                    mediaAssetId,
                    imageUrl,
                    safeName,
                    mimeType,
                    bytes.Length,
                    image.Width,
                    image.Height));
        }
        catch (UnknownImageFormatException)
        {
            return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Failure(
                Invalid("Image data is corrupted or cannot be decoded."));
        }
        catch (InvalidImageContentException)
        {
            return ApplicationResult<TenantAdminUserProfileImageUploadResponse>.Failure(
                Invalid("Image data is corrupted or cannot be decoded."));
        }
    }

    public async Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult.Failure(accessError);
        }

        var asset = await _repository.GetAsync(context.TenantId, mediaAssetId, cancellationToken);
        if (asset is null ||
            !string.Equals(asset.AssetType, "IMAGE", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(asset.AssetPurpose, AssetPurpose, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult.Failure(
                new ApplicationError("user.profile_image_not_found", "User profile image was not found."));
        }

        if (await _repository.IsAttachedAsync(context.TenantId, mediaAssetId, cancellationToken))
        {
            return ApplicationResult.Failure(
                new ApplicationError("user.profile_image_attached", "An image attached to a user cannot be deleted as staged media."));
        }

        if (string.Equals(asset.Status, "DELETED", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult.Success();
        }

        var now = _clock.UtcNow;
        asset.MarkDeletePending(context.UserId, now);
        await _repository.SaveChangesAsync(cancellationToken);
        try
        {
            await _storage.DeleteIfExistsAsync(asset.ContainerName, asset.StorageKey, cancellationToken);
            asset.MarkDeleted(context.UserId, now);
            await _repository.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var message = exception.Message[..Math.Min(500, exception.Message.Length)];
            asset.RecordDeletionFailure(message, now.AddMinutes(5), context.UserId, now);
            await _repository.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Failure(
                new ApplicationError("user.profile_image_delete_unavailable", "Image deletion is pending retry."));
        }
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context)
    {
        var hasPermission = context.HasPermission(TenantAdminUserPermissions.Create) ||
                            context.HasPermission(TenantAdminUserPermissions.Invite) ||
                            context.HasPermission(TenantAdminUserPermissions.Update) ||
                            context.HasPermission(TenantAdminUserPermissions.Manage);
        return context.TenantId == Guid.Empty || context.UserId == Guid.Empty || !hasPermission
            ? new ApplicationError("user.permission_denied", "Permission denied for user management.")
            : null;
    }

    private static ApplicationError Invalid(string message) =>
        new("user.profile_image_invalid", message, [new ApplicationFieldError("file", message)]);

    private static string NormalizeMime(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "image/jpg" => "image/jpeg",
        var mime => mime ?? string.Empty
    };

    private static bool IsAllowed(string mimeType, string extension) =>
        mimeType == "image/jpeg" && extension is ".jpg" or ".jpeg" ||
        mimeType == "image/png" && extension == ".png";

    private static bool MagicMatches(byte[] bytes, string mimeType) =>
        mimeType == "image/jpeg"
            ? bytes.Length > 2 && bytes[0] == 0xFF && bytes[1] == 0xD8
            : mimeType == "image/png" &&
              bytes.Length > 8 &&
              bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
}
