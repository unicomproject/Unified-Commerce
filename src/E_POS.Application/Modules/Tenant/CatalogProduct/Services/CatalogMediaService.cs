using System.Buffers.Binary;
using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class CatalogMediaService : ICatalogMediaService
{
    private const long MaxImageFileSizeBytes = 5 * 1024 * 1024;
    private const long MaxBrandLogoFileSizeBytes = 2 * 1024 * 1024;
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
        var accessError = ValidateProductMediaAccess(context);
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

        var activeCount = await _repository.CountActiveProductImagesAsync(
            context.TenantId,
            productId,
            cancellationToken);
        if (activeCount >= ProductConstants.MaxProductImages)
        {
            return ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError(
                "media.max_images_exceeded",
                $"A product can have at most {ProductConstants.MaxProductImages} images."));
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
            ActiveStatus,
            now);

        var existingImages = await _repository.GetActiveProductImagesAsync(
            context.TenantId,
            productId,
            cancellationToken);
        var hasPrimary = existingImages.Any(x => x.IsPrimaryImage);
        var isPrimary = request.IsPrimaryImage ?? (!hasPrimary && existingImages.Count == 0);

        var image = ProductImage.Create(
            id: productImageId,
            tenantId: context.TenantId,
            productId: productId,
            productVariantId: request.ProductVariantId,
            salesChannelId: request.SalesChannelId,
            mediaAssetId: mediaAssetId,
            altText: request.AltText,
            imagePurpose: purpose,
            sortOrder: Math.Max(0, request.SortOrder ?? existingImages.Count),
            isPrimaryImage: isPrimary,
            status: ActiveStatus,
            createdByTenantUserId: context.UserId,
            now: now);

        try
        {
            await _repository.AddMediaAssetAsync(mediaAsset, cancellationToken);
            await _repository.AddProductImageAsync(image, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await TryDeleteUploadedBlobAsync(uploadResult, cancellationToken);
            return ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError(
                "media.save_failed",
                "Failed to save image record: " + ex.Message));
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

    public async Task<ApplicationResult<StagedProductImageResponse>> StageProductImageAsync(
        TenantRequestContext context,
        MediaUploadFile file,
        Guid? uploadSessionId,
        CancellationToken cancellationToken)
    {
        _ = uploadSessionId;
        var accessError = ValidateProductMediaAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<StagedProductImageResponse>.Failure(accessError);
        }

        if (!_storage.IsConfigured)
        {
            return ApplicationResult<StagedProductImageResponse>.Failure(StorageNotConfigured());
        }

        var preparedResult = await PrepareImageAsync(file, cancellationToken);
        if (preparedResult.Error is not null)
        {
            return ApplicationResult<StagedProductImageResponse>.Failure(preparedResult.Error);
        }

        await using var content = preparedResult.Image!.Content;
        var mediaAssetId = Guid.NewGuid();
        var purpose = ProductConstants.ProductImagePurpose;
        var storageKey =
            $"tenants/{context.TenantId:D}/products/staged/{mediaAssetId:D}{preparedResult.Image.StorageExtension}";

        MediaObjectUploadResult uploadResult;
        try
        {
            uploadResult = await UploadToStorageAsync(
                context,
                mediaAssetId,
                storageKey,
                purpose,
                preparedResult.Image,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return ApplicationResult<StagedProductImageResponse>.Failure(new ApplicationError(
                "media.storage_unavailable",
                "Failed to upload media to storage: " + ex.Message));
        }

        var now = _dateTimeProvider.UtcNow;
        var mediaAsset = CreateMediaAsset(
            context,
            mediaAssetId,
            uploadResult,
            preparedResult.Image,
            purpose,
            "STAGED",
            now);

        try
        {
            await _repository.AddMediaAssetAsync(mediaAsset, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await TryDeleteUploadedBlobAsync(uploadResult, cancellationToken);
            return ApplicationResult<StagedProductImageResponse>.Failure(new ApplicationError(
                "media.save_failed",
                "Failed to save staged image record: " + ex.Message));
        }

        return ApplicationResult<StagedProductImageResponse>.Success(new StagedProductImageResponse(
            mediaAssetId,
            uploadResult.PublicUrl,
            preparedResult.Image.OriginalFileName,
            preparedResult.Image.MimeType,
            preparedResult.Image.FileSizeBytes,
            now,
            ProductConstants.StagedMediaStatus));
    }

    public async Task<ApplicationResult<ProductImagesMutationResponse>> ReorderProductImagesAsync(
        TenantRequestContext context,
        Guid productId,
        ReorderProductImagesRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateProductMediaManageAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(accessError);
        }

        var product = await _repository.GetProductForUpdateAsync(context.TenantId, productId, cancellationToken);
        if (product is null)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.product_not_found",
                "Product was not found."));
        }

        if (product.RowVersion != request.ExpectedRowVersion)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.concurrency_conflict",
                "Product was modified by another user. Refresh and try again."));
        }

        var images = await _repository.GetActiveProductImagesAsync(
            context.TenantId,
            productId,
            cancellationToken);
        var imageMap = images.ToDictionary(x => x.Id);
        var now = _dateTimeProvider.UtcNow;

        foreach (var item in request.Items)
        {
            if (!imageMap.TryGetValue(item.ProductImageId, out var image))
            {
                return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                    "media.validation_failed",
                    "Image validation failed.",
                    [new ApplicationFieldError("items", "One or more product images were not found.")]));
            }

            image.SetSortOrder(item.SortOrder, context.UserId, now);
        }

        if (request.PrimaryProductImageId.HasValue)
        {
            if (!imageMap.ContainsKey(request.PrimaryProductImageId.Value))
            {
                return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                    "media.validation_failed",
                    "Image validation failed.",
                    [new ApplicationFieldError("primaryProductImageId", "Primary product image was not found.")]));
            }

            foreach (var image in images)
            {
                image.SetPrimary(image.Id == request.PrimaryProductImageId.Value, context.UserId, now);
            }
        }

        product.IncrementRowVersion();
        await _repository.SaveChangesAsync(cancellationToken);

        var responseImages = await _repository.GetProductImageResponsesAsync(
            context.TenantId,
            productId,
            cancellationToken);

        return ApplicationResult<ProductImagesMutationResponse>.Success(
            new ProductImagesMutationResponse(productId, product.RowVersion, responseImages));
    }

    public async Task<ApplicationResult<ProductImagesMutationResponse>> DeleteProductImageAsync(
        TenantRequestContext context,
        Guid productId,
        Guid productImageId,
        long? expectedRowVersion,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateProductMediaManageAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(accessError);
        }

        var product = await _repository.GetProductForUpdateAsync(context.TenantId, productId, cancellationToken);
        if (product is null)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.product_not_found",
                "Product was not found."));
        }

        if (expectedRowVersion.HasValue && product.RowVersion != expectedRowVersion.Value)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.concurrency_conflict",
                "Product was modified by another user. Refresh and try again."));
        }

        var image = await _repository.GetProductImageAsync(
            context.TenantId,
            productId,
            productImageId,
            cancellationToken);
        if (image is null || image.Status == ProductConstants.DeletedStatus)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.validation_failed",
                "Product image was not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var wasPrimary = image.IsPrimaryImage;
        image.SoftDelete(context.UserId, now);

        if (wasPrimary)
        {
            var remaining = (await _repository.GetActiveProductImagesAsync(
                    context.TenantId,
                    productId,
                    cancellationToken))
                .Where(x => x.Id != productImageId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .ToList();

            var nextPrimary = remaining.FirstOrDefault();
            if (nextPrimary is not null)
            {
                nextPrimary.SetPrimary(true, context.UserId, now);
            }
        }

        product.IncrementRowVersion();
        await _repository.SaveChangesAsync(cancellationToken);

        var responseImages = await _repository.GetProductImageResponsesAsync(
            context.TenantId,
            productId,
            cancellationToken);

        return ApplicationResult<ProductImagesMutationResponse>.Success(
            new ProductImagesMutationResponse(productId, product.RowVersion, responseImages));
    }

    public async Task<ApplicationResult<ProductImagesMutationResponse>> ReplaceProductImagesAsync(
        TenantRequestContext context,
        Guid productId,
        long expectedRowVersion,
        IReadOnlyList<MediaUploadFile>? files,
        IReadOnlyList<Guid>? stagedMediaAssetIds,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateProductMediaManageAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(accessError);
        }

        if (!_storage.IsConfigured && files is { Count: > 0 })
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(StorageNotConfigured());
        }

        var product = await _repository.GetProductForUpdateAsync(context.TenantId, productId, cancellationToken);
        if (product is null)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.product_not_found",
                "Product was not found."));
        }

        if (product.RowVersion != expectedRowVersion)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.concurrency_conflict",
                "Product was modified by another user. Refresh and try again."));
        }

        var stagedIds = stagedMediaAssetIds?.Distinct().ToArray() ?? [];
        var uploadCount = files?.Count ?? 0;
        if (stagedIds.Length + uploadCount > ProductConstants.MaxProductImages)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.max_images_exceeded",
                $"A product can have at most {ProductConstants.MaxProductImages} images."));
        }

        if (stagedIds.Length + uploadCount == 0)
        {
            return ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError(
                "media.validation_failed",
                "Image validation failed.",
                [new ApplicationFieldError("files", "At least one image file or staged media asset is required.")]));
        }

        var preparedUploads = new List<(PreparedImageUpload Image, MediaObjectUploadResult Upload, Guid MediaAssetId)>();
        ApplicationError? replaceError = null;
        try
        {
            await _repository.ExecuteInTransactionAsync(async ct =>
            {
                var now = _dateTimeProvider.UtcNow;
                var existing = await _repository.GetActiveProductImagesAsync(context.TenantId, productId, ct);
                foreach (var image in existing)
                {
                    image.SoftDelete(context.UserId, now);
                }

                var sortOrder = 0;
                var assignPrimary = true;

                foreach (var stagedId in stagedIds)
                {
                    var asset = await _repository.GetMediaAssetAsync(context.TenantId, stagedId, ct);
                    if (asset is null ||
                        asset.Status != "STAGED" ||
                        asset.AssetPurpose != ProductConstants.ProductImagePurpose)
                    {
                        replaceError = new ApplicationError(
                            "media.validation_failed",
                            "Image validation failed.",
                            [new ApplicationFieldError(
                                "stagedMediaAssetIds",
                                "One or more staged media assets were not found or are not available.")]);
                        throw new InvalidOperationException("staged_media_invalid");
                    }

                    // Soft-deleted current images still count as linked until save; allow same-session reuse after soft delete.
                    var linkedElsewhere = await _repository.IsMediaAssetLinkedAsync(context.TenantId, stagedId, ct);
                    if (linkedElsewhere &&
                        !existing.Any(x => x.MediaAssetId == stagedId))
                    {
                        replaceError = new ApplicationError(
                            "media.validation_failed",
                            "Image validation failed.",
                            [new ApplicationFieldError(
                                "stagedMediaAssetIds",
                                "One or more staged media assets are already linked.")]);
                        throw new InvalidOperationException("staged_media_linked");
                    }

                    var productImage = ProductImage.Create(
                        Guid.NewGuid(),
                        context.TenantId,
                        productId,
                        null,
                        null,
                        stagedId,
                        null,
                        ProductConstants.ProductImagePurpose,
                        sortOrder++,
                        assignPrimary,
                        ActiveStatus,
                        context.UserId,
                        now);
                    assignPrimary = false;
                    asset.MarkActive(context.UserId, now);
                    await _repository.AddProductImageAsync(productImage, ct);
                }

                if (files is { Count: > 0 })
                {
                    foreach (var file in files)
                    {
                        var preparedResult = await PrepareImageAsync(file, ct);
                        if (preparedResult.Error is not null)
                        {
                            replaceError = preparedResult.Error;
                            throw new InvalidOperationException("prepare_failed");
                        }

                        var mediaAssetId = Guid.NewGuid();
                        var storageKey = BuildStorageKey(
                            context.TenantId,
                            "products",
                            productId,
                            "images",
                            mediaAssetId,
                            preparedResult.Image!.StorageExtension);

                        var uploadResult = await UploadToStorageAsync(
                            context,
                            mediaAssetId,
                            storageKey,
                            ProductConstants.ProductImagePurpose,
                            preparedResult.Image,
                            ct);
                        preparedUploads.Add((preparedResult.Image, uploadResult, mediaAssetId));

                        var mediaAsset = CreateMediaAsset(
                            context,
                            mediaAssetId,
                            uploadResult,
                            preparedResult.Image,
                            ProductConstants.ProductImagePurpose,
                            ActiveStatus,
                            now);

                        var productImage = ProductImage.Create(
                            Guid.NewGuid(),
                            context.TenantId,
                            productId,
                            null,
                            null,
                            mediaAssetId,
                            null,
                            ProductConstants.ProductImagePurpose,
                            sortOrder++,
                            assignPrimary,
                            ActiveStatus,
                            context.UserId,
                            now);
                        assignPrimary = false;

                        await _repository.AddMediaAssetAsync(mediaAsset, ct);
                        await _repository.AddProductImageAsync(productImage, ct);
                    }
                }

                product.IncrementRowVersion();
                await _repository.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            foreach (var prepared in preparedUploads)
            {
                await TryDeleteUploadedBlobAsync(prepared.Upload, cancellationToken);
            }

            return ApplicationResult<ProductImagesMutationResponse>.Failure(
                replaceError ?? new ApplicationError(
                    "media.validation_failed",
                    "Image replacement failed validation."));
        }
        catch
        {
            foreach (var prepared in preparedUploads)
            {
                await TryDeleteUploadedBlobAsync(prepared.Upload, cancellationToken);
            }

            throw;
        }

        var responseImages = await _repository.GetProductImageResponsesAsync(
            context.TenantId,
            productId,
            cancellationToken);

        return ApplicationResult<ProductImagesMutationResponse>.Success(
            new ProductImagesMutationResponse(productId, product.RowVersion, responseImages));
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
            ActiveStatus,
            now);

        category.UpdateImage(mediaAssetId, context.UserId, now);

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
        catch (Exception ex)
        {
            await TryDeleteUploadedBlobAsync(uploadResult, cancellationToken);
            return ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError(
                "media.save_failed",
                "Failed to save category image record: " + ex.Message));
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

        var preparedResult = await PrepareImageAsync(file, cancellationToken, MaxBrandLogoFileSizeBytes, allowWebP: false);
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
            ActiveStatus,
            now);

        brand.UpdateLogo(mediaAssetId, context.UserId, now);

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
        catch (Exception ex)
        {
            await TryDeleteUploadedBlobAsync(uploadResult, cancellationToken);
            return ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError(
                "media.save_failed",
                "Failed to save brand image record: " + ex.Message));
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
        CancellationToken cancellationToken,
        long maxFileSizeBytes = MaxImageFileSizeBytes,
        bool allowWebP = true)
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

        if (file.Length > maxFileSizeBytes)
        {
            return PrepareImageResult.Failed(new ApplicationError(
                "media.file_size_exceeded",
                "Image file size exceeds the allowed limit.",
                [new ApplicationFieldError("file", $"Image file size exceeds the allowed {maxFileSizeBytes / (1024 * 1024)} MB limit.")]));
        }

        var declaredContentType = NormalizeContentType(file.ContentType);
        if (!string.IsNullOrWhiteSpace(declaredContentType) && declaredContentType != "application/octet-stream" && !IsAllowedMimeType(declaredContentType, allowWebP))
        {
            return PrepareImageResult.Failed(new ApplicationError(
                "media.unsupported_media_type",
                allowWebP ? "Only JPEG, PNG and WebP images are allowed." : "Only JPEG and PNG images are allowed.",
                [new ApplicationFieldError("contentType", allowWebP ? "Only JPEG, PNG and WebP images are allowed." : "Only JPEG and PNG images are allowed.")]));
        }

        var originalFileName = NormalizeFileName(file.FileName);
        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();

        var memory = new MemoryStream(capacity: (int)Math.Min(file.Length, maxFileSizeBytes));
        await file.Content.CopyToAsync(memory, cancellationToken);
        if (memory.Length <= 0)
        {
            await memory.DisposeAsync();
            return PrepareImageResult.Failed(ValidationFailed([
                new ApplicationFieldError("file", "Image file cannot be empty.")
            ]));
        }

        if (memory.Length > maxFileSizeBytes)
        {
            await memory.DisposeAsync();
            return PrepareImageResult.Failed(new ApplicationError(
                "media.file_size_exceeded",
                "Image file size exceeds the allowed limit.",
                [new ApplicationFieldError("file", $"Image file size exceeds the allowed {maxFileSizeBytes / (1024 * 1024)} MB limit.")]));
        }

        var bytes = memory.ToArray();
        var mimeType = declaredContentType;

        if (!TryReadImageDimensions(bytes, ref mimeType, out var widthPx, out var heightPx))
        {
            await memory.DisposeAsync();
            return PrepareImageResult.Failed(ValidationFailed([
                new ApplicationFieldError("file", "Image signature or dimensions are invalid for the supplied MIME type.")
            ]));
        }

        if (!IsAllowedMimeType(mimeType, allowWebP))
        {
            await memory.DisposeAsync();
            return PrepareImageResult.Failed(new ApplicationError(
                "media.unsupported_media_type",
                allowWebP ? "Only JPEG, PNG and WebP images are allowed." : "Only JPEG and PNG images are allowed.",
                [new ApplicationFieldError("contentType", allowWebP ? "Only JPEG, PNG and WebP images are allowed." : "Only JPEG and PNG images are allowed.")]));
        }

        // If byte signature auto-detected the true format (e.g. WebP/PNG) or extension is missing/mismatched for auto-probed type, adjust fileExtension
        if (string.IsNullOrWhiteSpace(fileExtension) || fileExtension == "." || (mimeType != declaredContentType && IsAllowedMimeType(mimeType, allowWebP)))
        {
            fileExtension = ResolveStorageExtension(mimeType);
        }
        else if (!IsAllowedExtensionForMimeType(fileExtension, mimeType, allowWebP))
        {
            await memory.DisposeAsync();
            return PrepareImageResult.Failed(ValidationFailed([
                new ApplicationFieldError("fileName", "File extension does not match an allowed image type.")
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
        string status,
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
            status,
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

    private static ApplicationError? ValidateProductMediaAccess(TenantRequestContext context) =>
        ValidateProductMediaManageAccess(context);

    private static ApplicationError? ValidateProductMediaManageAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("media.invalid_tenant_context", "Invalid tenant context.");
        }

        // Canonical catalogue only — no tenant.products.* and no products.update substitute.
        return context.HasPermission(ProductConstants.MediaManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateProductAccess(TenantRequestContext context) =>
        ValidateProductMediaManageAccess(context);

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

    private static bool IsAllowedMimeType(string mimeType, bool allowWebP = true) =>
        mimeType is "image/jpeg" or "image/png" || (allowWebP && mimeType == "image/webp");

    private static bool IsAllowedExtensionForMimeType(string extension, string mimeType, bool allowWebP = true) =>
        extension switch
        {
            ".jpg" or ".jpeg" or ".jfif" or ".pjpeg" or ".pjp" => mimeType is "image/jpeg" or "image/pjpeg" or "image/jfif",
            ".png" => mimeType == "image/png",
            ".webp" => allowWebP && mimeType == "image/webp",
            _ => true
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
        ref string mimeType,
        out int? widthPx,
        out int? heightPx)
    {
        widthPx = null;
        heightPx = null;

        if (mimeType switch
            {
                "image/png" => TryReadPngDimensions(bytes, out widthPx, out heightPx),
                "image/jpeg" or "image/pjpeg" or "image/jfif" => TryReadJpegDimensions(bytes, out widthPx, out heightPx),
                "image/webp" => TryReadWebpDimensions(bytes, out widthPx, out heightPx),
                _ => false
            })
        {
            return true;
        }

        if (TryReadPngDimensions(bytes, out widthPx, out heightPx))
        {
            mimeType = "image/png";
            return true;
        }

        if (TryReadJpegDimensions(bytes, out widthPx, out heightPx))
        {
            mimeType = "image/jpeg";
            return true;
        }

        if (TryReadWebpDimensions(bytes, out widthPx, out heightPx))
        {
            mimeType = "image/webp";
            return true;
        }

        return false;
    }

    private static bool TryReadGifDimensions(byte[] bytes, out int? widthPx, out int? heightPx)
    {
        widthPx = null;
        heightPx = null;

        if (bytes.Length < 10 ||
            !HasAscii(bytes, 0, "GIF87a") && !HasAscii(bytes, 0, "GIF89a"))
        {
            return false;
        }

        widthPx = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6, 2));
        heightPx = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2));
        return widthPx > 0 && heightPx > 0;
    }

    private static bool TryReadBmpDimensions(byte[] bytes, out int? widthPx, out int? heightPx)
    {
        widthPx = null;
        heightPx = null;

        if (bytes.Length < 26 || bytes[0] != 0x42 || bytes[1] != 0x4D)
        {
            return false;
        }

        widthPx = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(18, 4)));
        heightPx = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(22, 4)));
        return widthPx > 0 && heightPx > 0;
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
