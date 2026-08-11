using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Domain.Modules.Shared.Media.Constants;
using E_POS.Domain.Modules.Shared.Media.Entities;
using SixLabors.ImageSharp;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Services;

public sealed class PosLoginBrandingMediaService : IPosLoginBrandingMediaService
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private const long MaxPixels = 16_000_000;
    private readonly IPosLoginBrandingMediaRepository _repository;
    private readonly IMediaObjectStorage _storage;
    private readonly IDateTimeProvider _clock;

    public PosLoginBrandingMediaService(
        IPosLoginBrandingMediaRepository repository,
        IMediaObjectStorage storage,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _storage = storage;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosLoginBrandingMediaUploadResponse>> UploadAsync(
        TenantRequestContext context,
        string purpose,
        MediaUploadFile file,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty ||
            !context.HasPermission(PosLoginBrandingService.ManagePermission))
            return Failure("pos_login_branding.permission_denied", "Permission denied.");

        var normalizedPurpose = purpose?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizedPurpose is not MediaAssetPurposes.PosLoginBackground and not MediaAssetPurposes.PosLoginHero)
            return Invalid("purpose", "Purpose must be POS_LOGIN_BACKGROUND or POS_LOGIN_HERO.");
        if (!_storage.IsConfigured)
            return Failure("pos_login_branding.media_storage_unavailable", "Image storage is not configured.");
        if (file.Length is <= 0 or > MaxBytes)
            return Invalid("file", "Image file must be between 1 byte and 5 MB.");

        var name = Path.GetFileName(file.FileName ?? string.Empty).Trim();
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var mime = NormalizeMime(file.ContentType);
        if (!Allowed(mime, extension))
            return Invalid("file", "Only JPG, JPEG, PNG and WEBP images with a matching MIME type are allowed.");

        await using var memory = new MemoryStream((int)file.Length);
        await file.Content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        if (bytes.Length != file.Length || !MagicMatches(bytes, mime))
            return Invalid("file", "Image signature does not match the declared image type.");

        try
        {
            using var image = Image.Load(bytes);
            if ((long)image.Width * image.Height > MaxPixels)
                return Invalid("file", "Image dimensions exceed the 16 MP limit.");

            var id = Guid.NewGuid();
            var now = _clock.UtcNow;
            var safeName = $"pos-login-{normalizedPurpose.ToLowerInvariant().Replace('_', '-')}-{id:N}{extension}";
            var key = $"tenants/{context.TenantId:D}/pos-login-branding/{normalizedPurpose.ToLowerInvariant()}/{id:N}{extension}";
            memory.Position = 0;
            var uploaded = await _storage.UploadAsync(
                new MediaObjectUploadRequest(
                    key,
                    memory,
                    mime,
                    new Dictionary<string, string>
                    {
                        ["tenant-id"] = context.TenantId.ToString("D"),
                        ["purpose"] = normalizedPurpose
                    }),
                cancellationToken);

            var asset = MediaAsset.Create(
                id,
                context.TenantId,
                uploaded.ContainerName,
                uploaded.StorageKey,
                uploaded.PublicUrl,
                safeName,
                mime,
                extension,
                bytes.Length,
                image.Width,
                image.Height,
                Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                "IMAGE",
                normalizedPurpose,
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

            return ApplicationResult<PosLoginBrandingMediaUploadResponse>.Success(
                new PosLoginBrandingMediaUploadResponse(
                    id,
                    normalizedPurpose,
                    uploaded.PublicUrl,
                    safeName,
                    mime,
                    bytes.Length,
                    image.Width,
                    image.Height));
        }
        catch (UnknownImageFormatException)
        {
            return Invalid("file", "Image data is corrupted or cannot be decoded.");
        }
        catch (InvalidImageContentException)
        {
            return Invalid("file", "Image data is corrupted or cannot be decoded.");
        }
    }

    private static ApplicationResult<PosLoginBrandingMediaUploadResponse> Failure(string code, string message) =>
        ApplicationResult<PosLoginBrandingMediaUploadResponse>.Failure(new ApplicationError(code, message));

    private static ApplicationResult<PosLoginBrandingMediaUploadResponse> Invalid(string field, string message) =>
        ApplicationResult<PosLoginBrandingMediaUploadResponse>.Failure(
            new ApplicationError(
                "pos_login_branding.media_invalid",
                message,
                [new ApplicationFieldError(field, message)]));

    private static string NormalizeMime(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "image/jpg" => "image/jpeg",
        var mime => mime ?? string.Empty
    };

    private static bool Allowed(string mime, string extension) =>
        mime == "image/jpeg" && extension is ".jpg" or ".jpeg" ||
        mime == "image/png" && extension == ".png" ||
        mime == "image/webp" && extension == ".webp";

    private static bool MagicMatches(byte[] bytes, string mime) => mime switch
    {
        "image/jpeg" => bytes.Length > 2 && bytes[0] == 0xFF && bytes[1] == 0xD8,
        "image/png" => bytes.Length > 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/webp" => bytes.Length > 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
}
