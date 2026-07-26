using System.Buffers.Binary;
using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class CatalogMediaService : ICatalogMediaService
{
    private const long MaxImageFileSizeBytes = 5 * 1024 * 1024;
    private const string AssetTypeImage = "IMAGE";
    private const string ActiveStatus = "ACTIVE";

    private static readonly ApplicationError PermissionDenied = new(
        "media.permission_denied",
        "Permission denied for media upload.");

    private readonly ICatalogMediaRepository _repository;
    private readonly IMediaObjectStorage _storage;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CatalogMediaService(
        ICatalogMediaRepository repository,
        IMediaObjectStorage storage,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _storage = storage;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<MediaAssetUploadResponse>> UploadProductImageAsync(
        TenantRequestContext context,
        Guid productId,
        ProductImageUploadRequest request,
        MediaUploadFile file,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateProductAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(accessError);
        }

        if (!_storage.IsConfigured)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(StorageNotConfigured());
        }

        if (!await _repository.ProductExistsAsync(context.TenantId, productId, cancellationToken))
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError(
                "media.product_not_found",
                "Product was not found."));
        }

        if (request.ProductVariantId.HasValue &&
            !await _repository.ProductVariantExistsAsync(
                context.TenantId,
                productId,
                request.ProductVariantId.Value,
                cancellationToken))
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError(
                "media.variant_not_found",
                "Product variant was not found for this product."));
        }

        var preparedResult = await PrepareImageAsync(file, cancellationToken);
        if (preparedResult.Error is not null)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(preparedResult.Error);
        }

        await using var content = preparedResult.Image!.Content;
        var mediaAssetId = Guid.NewGuid();
        var productImageId = Guid.NewGuid();
        var purpose = NormalizePurpose(
            request.ImagePurpose,
            request.ProductVariantId.HasValue ? "VARIANT" : "PRODUCT");
        var storageKey = BuildStorageKey(
            context.TenantId,
            "products",
            productId,
            request.ProductVariantId.HasValue ? $"variants/{request.ProductVariantId.Value:D}/images" : "images",
            mediaAssetId,
            preparedResult.Image.StorageExtension);

        var uploadResult = await UploadToStorageAsync(
            context,
            mediaAssetId,
            storageKey,
            purpose,
            preparedResult.Image,
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var mediaAsset = CreateMediaAsset(
            context,
            mediaAssetId,
            uploadResult,
            preparedResult.Image,
            purpose,
            now);

        var image = ProductImage.Create(
            id: productImageId,
            tenantId: context.TenantId,
            productId: productId,
            productVariantId: request.ProductVariantId,
            salesChannelId: request.SalesChannelId,
            imageStorageKey: uploadResult.StorageKey,
            imageUrl: uploadResult.PublicUrl,
            altText: request.AltText,
            imagePurpose: purpose,
            mimeType: preparedResult.Image.MimeType,
            fileSizeBytes: preparedResult.Image.FileSizeBytes,
            widthPx: preparedResult.Image.WidthPx,
            heightPx: preparedResult.Image.HeightPx,
            checksumHash: preparedResult.Image.ChecksumHash,
            sortOrder: Math.Max(0, request.SortOrder ?? 0),
            isPrimaryImage: request.IsPrimaryImage ?? false,
            status: ActiveStatus,
            createdByTenantUserId: context.UserId,
            now: now,
            mediaAssetId: mediaAssetId);

        try
        {
            await _repository.AddMediaAssetAsync(mediaAsset, cancellationToken);
            await _repository.AddProductImageAsync(image, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedBlobAsync(uploadResult, cancellationToken);
            throw;
        }

        return ApplicationResult<MediaAssetUploadResponse>.Success(new MediaAssetUploadResponse(
            mediaAssetId,
            productImageId,
            productId,
            request.ProductVariantId,
            null,
            null,
            uploadResult.ContainerName,
            uploadResult.StorageKey,
            uploadResult.PublicUrl,
            uploadResult.PublicUrl,
            null,
            preparedResult.Image.OriginalFileName,
            preparedResult.Image.MimeType,
            preparedResult.Image.FileExtension,
            preparedResult.Image.FileSizeBytes,
            preparedResult.Image.WidthPx,
            preparedResult.Image.HeightPx,
            preparedResult.Image.ChecksumHash));
    }

    public async Task<ApplicationResult<MediaAssetUploadResponse>> UploadCategoryImageAsync(
        TenantRequestContext context,
        Guid categoryId,
        MediaUploadFile file,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateCategoryAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(accessError);
        }

        if (!_storage.IsConfigured)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(StorageNotConfigured());
        }

        var category = await _repository.GetCategoryForImageUpdateAsync(
            context.TenantId,
            categoryId,
            cancellationToken);
        if (category is null)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError(
                "media.category_not_found",
                "Category was not found."));
        }

        var previousMediaAssetId = category.ImageMediaAssetId;

        var preparedResult = await PrepareImageAsync(file, cancellationToken);
        if (preparedResult.Error is not null)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(preparedResult.Error);
        }

        await using var content = preparedResult.Image!.Content;
        var mediaAssetId = Guid.NewGuid();
        const string purpose = "CATEGORY";
        var storageKey = BuildStorageKey(
            context.TenantId,
            "categories",
            categoryId,
            "image",
            mediaAssetId,
            preparedResult.Image.StorageExtension);

        var uploadResult = await UploadToStorageAsync(
            context,
            mediaAssetId,
            storageKey,
            purpose,
            preparedResult.Image,
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var mediaAsset = CreateMediaAsset(
            context,
            mediaAssetId,
            uploadResult,
            preparedResult.Image,
            purpose,
            now);

        category.UpdateImage(uploadResult.PublicUrl, mediaAssetId, context.UserId, now);

        try
        {
            await _repository.AddMediaAssetAsync(mediaAsset, cancellationToken);
            if (previousMediaAssetId.HasValue)
            {
                await _repository.MarkMediaAssetInactiveAsync(
                    context.TenantId,
                    previousMediaAssetId.Value,
                    context.UserId,
                    now,
                    cancellationToken);
            }

            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedBlobAsync(uploadResult, cancellationToken);
            throw;
        }

        return ApplicationResult<MediaAssetUploadResponse>.Success(new MediaAssetUploadResponse(
            mediaAssetId,
            null,
            null,
            null,
            categoryId,
            null,
            uploadResult.ContainerName,
            uploadResult.StorageKey,
            uploadResult.PublicUrl,
            uploadResult.PublicUrl,
            null,
            preparedResult.Image.OriginalFileName,
            preparedResult.Image.MimeType,
            preparedResult.Image.FileExtension,
            preparedResult.Image.FileSizeBytes,
            preparedResult.Image.WidthPx,
            preparedResult.Image.HeightPx,
            preparedResult.Image.ChecksumHash));
    }

    public async Task<ApplicationResult<MediaAssetUploadResponse>> UploadBrandLogoAsync(
        TenantRequestContext context,
        Guid brandId,
        MediaUploadFile file,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateBrandAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(accessError);
        }

        if (!_storage.IsConfigured)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(StorageNotConfigured());
        }

        var brand = await _repository.GetBrandForLogoUpdateAsync(
            context.TenantId,
            brandId,
            cancellationToken);
        if (brand is null)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError(
                "media.brand_not_found",
                "Brand was not found."));
        }

        var previousMediaAssetId = brand.LogoMediaAssetId;

        var preparedResult = await PrepareImageAsync(file, cancellationToken);
        if (preparedResult.Error is not null)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(preparedResult.Error);
        }

        await using var content = preparedResult.Image!.Content;
        var mediaAssetId = Guid.NewGuid();
        const string purpose = "BRAND_LOGO";
        var storageKey = BuildStorageKey(
            context.TenantId,
            "brands",
            brandId,
            "logo",
            mediaAssetId,
            preparedResult.Image.StorageExtension);

        var uploadResult = await UploadToStorageAsync(
            context,
            mediaAssetId,
            storageKey,
            purpose,
            preparedResult.Image,
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var mediaAsset = CreateMediaAsset(
            context,
            mediaAssetId,
            uploadResult,
            preparedResult.Image,
            purpose,
            now);

        brand.UpdateLogo(uploadResult.PublicUrl, mediaAssetId, context.UserId, now);

        try
        {
            await _repository.AddMediaAssetAsync(mediaAsset, cancellationToken);
            if (previousMediaAssetId.HasValue)
            {
                await _repository.MarkMediaAssetInactiveAsync(
                    context.TenantId,
                    previousMediaAssetId.Value,
                    context.UserId,
                    now,
                    cancellationToken);
            }

            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedBlobAsync(uploadResult, cancellationToken);
            throw;
        }

        return ApplicationResult<MediaAssetUploadResponse>.Success(new MediaAssetUploadResponse(
            mediaAssetId,
            null,
            null,
            null,
            null,
            brandId,
            uploadResult.ContainerName,
            uploadResult.StorageKey,
            uploadResult.PublicUrl,
            null,
            uploadResult.PublicUrl,
            preparedResult.Image.OriginalFileName,
            preparedResult.Image.MimeType,
            preparedResult.Image.FileExtension,
            preparedResult.Image.FileSizeBytes,
            preparedResult.Image.WidthPx,
            preparedResult.Image.HeightPx,
            preparedResult.Image.ChecksumHash));
    }

    private async Task<PrepareImageResult> PrepareImageAsync(
        MediaUploadFile file,
        CancellationToken cancellationToken)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (file.Content == Stream.Null)
        {
            fieldErrors.Add(new ApplicationFieldError("file", "Image file is required."));
        }

        if (file.Length <= 0)
        {
            fieldErrors.Add(new ApplicationFieldError("file", "Image file cannot be empty."));
        }

        if (file.Length > MaxImageFileSizeBytes)
        {
            fieldErrors.Add(new ApplicationFieldError("file", "Image file size exceeds the allowed limit."));
        }

        var mimeType = NormalizeContentType(file.ContentType);
        if (!IsAllowedMimeType(mimeType))
        {
            fieldErrors.Add(new ApplicationFieldError("contentType", "Only JPEG, PNG and WebP images are allowed."));
        }

        var originalFileName = NormalizeFileName(file.FileName);
        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!IsAllowedExtensionForMimeType(fileExtension, mimeType))
        {
            fieldErrors.Add(new ApplicationFieldError("fileName", "File extension does not match an allowed image type."));
        }

        if (fieldErrors.Count > 0)
        {
            return PrepareImageResult.Failed(ValidationFailed(fieldErrors));
        }

        var memory = new MemoryStream(capacity: (int)Math.Min(file.Length, MaxImageFileSizeBytes));
        await file.Content.CopyToAsync(memory, cancellationToken);
        if (memory.Length <= 0)
        {
            await memory.DisposeAsync();
            return PrepareImageResult.Failed(ValidationFailed([
                new ApplicationFieldError("file", "Image file cannot be empty.")
            ]));
        }

        if (memory.Length > MaxImageFileSizeBytes)
        {
            await memory.DisposeAsync();
            return PrepareImageResult.Failed(ValidationFailed([
                new ApplicationFieldError("file", "Image file size exceeds the allowed limit.")
            ]));
        }

        var bytes = memory.ToArray();
        if (!TryReadImageDimensions(bytes, mimeType, out var widthPx, out var heightPx))
        {
            await memory.DisposeAsync();
            return PrepareImageResult.Failed(ValidationFailed([
                new ApplicationFieldError("file", "Image signature or dimensions are invalid for the supplied MIME type.")
            ]));
        }

        var checksumHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        memory.Position = 0;

        return PrepareImageResult.Success(new PreparedImageUpload(
            memory,
            originalFileName,
            mimeType,
            fileExtension,
            ResolveStorageExtension(mimeType),
            memory.Length,
            widthPx,
            heightPx,
            checksumHash));
    }

    private async Task<MediaObjectUploadResult> UploadToStorageAsync(
        TenantRequestContext context,
        Guid mediaAssetId,
        string storageKey,
        string purpose,
        PreparedImageUpload image,
        CancellationToken cancellationToken)
    {
        image.Content.Position = 0;
        return await _storage.UploadAsync(
            new MediaObjectUploadRequest(
                storageKey,
                image.Content,
                image.MimeType,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["tenant_id"] = context.TenantId.ToString("D"),
                    ["media_asset_id"] = mediaAssetId.ToString("D"),
                    ["asset_type"] = AssetTypeImage,
                    ["asset_purpose"] = purpose,
                    ["checksum_hash"] = image.ChecksumHash
                }),
            cancellationToken);
    }

    private static MediaAsset CreateMediaAsset(
        TenantRequestContext context,
        Guid mediaAssetId,
        MediaObjectUploadResult uploadResult,
        PreparedImageUpload image,
        string purpose,
        DateTimeOffset now)
    {
        return MediaAsset.Create(
            mediaAssetId,
            context.TenantId,
            uploadResult.ContainerName,
            uploadResult.StorageKey,
            uploadResult.PublicUrl,
            image.OriginalFileName,
            image.MimeType,
            image.FileExtension,
            image.FileSizeBytes,
            image.WidthPx,
            image.HeightPx,
            image.ChecksumHash,
            AssetTypeImage,
            purpose,
            ActiveStatus,
            context.UserId,
            now);
    }

    private async Task TryDeleteUploadedBlobAsync(
        MediaObjectUploadResult uploadResult,
        CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteIfExistsAsync(
                uploadResult.ContainerName,
                uploadResult.StorageKey,
                cancellationToken);
        }
        catch
        {
            // Best-effort cleanup only. Preserve the original DB exception for the API error pipeline.
        }
    }

    private static ApplicationError? ValidateProductAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("media.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminProductPermissions.Update) ||
               context.HasPermission(ProductConstants.UpdatePermission) ||
               context.HasPermission(ProductConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateCategoryAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("media.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(CategoryConstants.UpdatePermission) ||
               context.HasPermission(CategoryConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateBrandAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("media.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(BrandConstants.UpdatePermission) ||
               context.HasPermission(BrandConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError ValidationFailed(IReadOnlyList<ApplicationFieldError> fieldErrors) =>
        new("media.validation_failed", "Image validation failed.", fieldErrors);

    private static ApplicationError StorageNotConfigured() =>
        new("media.storage_not_configured", "Media storage is not configured.");

    private static string BuildStorageKey(
        Guid tenantId,
        string ownerSegment,
        Guid ownerId,
        string purposeSegment,
        Guid mediaAssetId,
        string extension)
    {
        return string.Join(
            '/',
            "tenants",
            tenantId.ToString("D"),
            ownerSegment,
            ownerId.ToString("D"),
            purposeSegment,
            $"{mediaAssetId:D}{extension}");
    }

    private static string NormalizePurpose(string? purpose, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(purpose) ? fallback : purpose.Trim();
        value = value.Replace(' ', '_').Replace('-', '_').ToUpperInvariant();
        return value.Length <= 40 ? value : value[..40];
    }

    private static string NormalizeContentType(string? contentType)
    {
        var value = contentType?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Trim());
        return string.IsNullOrWhiteSpace(name) ? "upload" : name;
    }

    private static bool IsAllowedMimeType(string mimeType) =>
        mimeType is "image/jpeg" or "image/png" or "image/webp";

    private static bool IsAllowedExtensionForMimeType(string extension, string mimeType) =>
        mimeType switch
        {
            "image/jpeg" => extension is ".jpg" or ".jpeg",
            "image/png" => extension == ".png",
            "image/webp" => extension == ".webp",
            _ => false
        };

    private static string ResolveStorageExtension(string mimeType) =>
        mimeType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

    private static bool TryReadImageDimensions(
        byte[] bytes,
        string mimeType,
        out int? widthPx,
        out int? heightPx)
    {
        widthPx = null;
        heightPx = null;

        return mimeType switch
        {
            "image/png" => TryReadPngDimensions(bytes, out widthPx, out heightPx),
            "image/jpeg" => TryReadJpegDimensions(bytes, out widthPx, out heightPx),
            "image/webp" => TryReadWebpDimensions(bytes, out widthPx, out heightPx),
            _ => false
        };
    }

    private static bool TryReadPngDimensions(byte[] bytes, out int? widthPx, out int? heightPx)
    {
        widthPx = null;
        heightPx = null;

        if (bytes.Length < 24 ||
            bytes[0] != 0x89 ||
            bytes[1] != 0x50 ||
            bytes[2] != 0x4E ||
            bytes[3] != 0x47 ||
            bytes[4] != 0x0D ||
            bytes[5] != 0x0A ||
            bytes[6] != 0x1A ||
            bytes[7] != 0x0A ||
            bytes[12] != 0x49 ||
            bytes[13] != 0x48 ||
            bytes[14] != 0x44 ||
            bytes[15] != 0x52)
        {
            return false;
        }

        widthPx = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        heightPx = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
        return widthPx > 0 && heightPx > 0;
    }

    private static bool TryReadJpegDimensions(byte[] bytes, out int? widthPx, out int? heightPx)
    {
        widthPx = null;
        heightPx = null;

        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            return false;
        }

        var offset = 2;
        while (offset + 9 < bytes.Length)
        {
            while (offset < bytes.Length && bytes[offset] != 0xFF)
            {
                offset++;
            }

            while (offset < bytes.Length && bytes[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                return false;
            }

            var marker = bytes[offset++];
            if (marker is 0xD9 or 0xDA)
            {
                return false;
            }

            if (offset + 1 >= bytes.Length)
            {
                return false;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > bytes.Length)
            {
                return false;
            }

            if (IsJpegStartOfFrame(marker))
            {
                if (offset + 7 >= bytes.Length)
                {
                    return false;
                }

                heightPx = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 3, 2));
                widthPx = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 5, 2));
                return widthPx > 0 && heightPx > 0;
            }

            offset += segmentLength;
        }

        return false;
    }

    private static bool IsJpegStartOfFrame(byte marker) =>
        marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;

    private static bool TryReadWebpDimensions(byte[] bytes, out int? widthPx, out int? heightPx)
    {
        widthPx = null;
        heightPx = null;

        if (bytes.Length < 30 ||
            !HasAscii(bytes, 0, "RIFF") ||
            !HasAscii(bytes, 8, "WEBP"))
        {
            return false;
        }

        if (HasAscii(bytes, 12, "VP8X"))
        {
            widthPx = 1 + bytes[24] + (bytes[25] << 8) + (bytes[26] << 16);
            heightPx = 1 + bytes[27] + (bytes[28] << 8) + (bytes[29] << 16);
            return widthPx > 0 && heightPx > 0;
        }

        if (HasAscii(bytes, 12, "VP8L") && bytes.Length >= 25 && bytes[20] == 0x2F)
        {
            var b0 = bytes[21];
            var b1 = bytes[22];
            var b2 = bytes[23];
            var b3 = bytes[24];
            widthPx = 1 + (((b1 & 0x3F) << 8) | b0);
            heightPx = 1 + (((b3 & 0x0F) << 10) | (b2 << 2) | ((b1 & 0xC0) >> 6));
            return widthPx > 0 && heightPx > 0;
        }

        if (HasAscii(bytes, 12, "VP8 ") &&
            bytes.Length >= 30 &&
            bytes[23] == 0x9D &&
            bytes[24] == 0x01 &&
            bytes[25] == 0x2A)
        {
            widthPx = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(26, 2)) & 0x3FFF;
            heightPx = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28, 2)) & 0x3FFF;
            return widthPx > 0 && heightPx > 0;
        }

        return false;
    }

    private static bool HasAscii(byte[] bytes, int offset, string value)
    {
        if (offset < 0 || bytes.Length < offset + value.Length)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (bytes[offset + i] != value[i])
            {
                return false;
            }
        }

        return true;
    }

    private sealed record PreparedImageUpload(
        MemoryStream Content,
        string OriginalFileName,
        string MimeType,
        string FileExtension,
        string StorageExtension,
        long FileSizeBytes,
        int? WidthPx,
        int? HeightPx,
        string ChecksumHash);

    private sealed record PrepareImageResult(PreparedImageUpload? Image, ApplicationError? Error)
    {
        public static PrepareImageResult Success(PreparedImageUpload image) => new(image, null);
        public static PrepareImageResult Failed(ApplicationError error) => new(null, error);
    }
}
