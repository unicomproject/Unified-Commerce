using System.Text.Json;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Domain.Modules.Shared.Media.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Services;

public sealed class PosLoginBrandingService : IPosLoginBrandingService
{
    public const string ManagePermission = "tenant.settings.manage";
    private static readonly string[] Keys =
    [
        TenantSettingKeys.PosLoginSystemName,
        TenantSettingKeys.PosLoginDescription,
        TenantSettingKeys.PosLoginSubtitleTemplate,
        TenantSettingKeys.PosLoginBackgroundMode,
        TenantSettingKeys.PosLoginBackgroundColor,
        TenantSettingKeys.PosLoginBackgroundMediaAssetId,
        TenantSettingKeys.PosLoginHeroMediaAssetId
    ];

    private readonly IPosLoginBrandingRepository _repository;

    public PosLoginBrandingService(IPosLoginBrandingRepository repository) => _repository = repository;

    public async Task<ApplicationResult<PublicPosLoginBrandingResponse>> GetPublicAsync(
        string tenantSlug,
        CancellationToken cancellationToken)
    {
        var slug = tenantSlug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (slug.Length is < 3 or > 100 || slug.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-')))
            return Failure<PublicPosLoginBrandingResponse>("pos_login_branding.invalid_slug", "Invalid tenant branding identifier.");

        var tenant = await _repository.FindActiveTenantBySlugAsync(slug, cancellationToken);
        if (tenant is null)
            return Failure<PublicPosLoginBrandingResponse>("pos_login_branding.unavailable", "Login branding is unavailable.");

        return ApplicationResult<PublicPosLoginBrandingResponse>.Success(
            await ResolveEffectiveAsync(tenant, cancellationToken));
    }

    public async Task<ApplicationResult<TenantAdminPosLoginBrandingResponse>> GetAdminAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ManagePermission))
            return Failure<TenantAdminPosLoginBrandingResponse>("pos_login_branding.permission_denied", "Permission denied.");

        var tenant = await _repository.FindTenantAsync(context.TenantId, cancellationToken);
        if (tenant is null)
            return Failure<TenantAdminPosLoginBrandingResponse>("pos_login_branding.unavailable", "Login branding is unavailable.");

        return ApplicationResult<TenantAdminPosLoginBrandingResponse>.Success(
            await BuildAdminResponseAsync(tenant, cancellationToken));
    }

    public async Task<ApplicationResult<TenantAdminPosLoginBrandingResponse>> UpdateAdminAsync(
        TenantRequestContext context,
        UpdatePosLoginBrandingRequest request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(ManagePermission))
            return Failure<TenantAdminPosLoginBrandingResponse>("pos_login_branding.permission_denied", "Permission denied.");

        var validation = Validate(request);
        if (validation is not null)
            return ApplicationResult<TenantAdminPosLoginBrandingResponse>.Failure(validation);

        var tenant = await _repository.FindTenantAsync(context.TenantId, cancellationToken);
        if (tenant is null)
            return Failure<TenantAdminPosLoginBrandingResponse>("pos_login_branding.unavailable", "Login branding is unavailable.");

        if (request.BackgroundMediaAssetId is { } backgroundId)
        {
            var media = await _repository.FindMediaAsync(backgroundId, cancellationToken);
            var error = PosLoginBrandingValidator.ValidateMedia(media, context.TenantId, MediaAssetPurposes.PosLoginBackground, "pos_login_branding.background_media_invalid");
            if (error is not null) return ApplicationResult<TenantAdminPosLoginBrandingResponse>.Failure(error);
        }

        if (request.HeroMediaAssetId is { } heroId)
        {
            var media = await _repository.FindMediaAsync(heroId, cancellationToken);
            var error = PosLoginBrandingValidator.ValidateMedia(media, context.TenantId, MediaAssetPurposes.PosLoginHero, "pos_login_branding.hero_media_invalid");
            if (error is not null) return ApplicationResult<TenantAdminPosLoginBrandingResponse>.Failure(error);
        }

        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [TenantSettingKeys.PosLoginSystemName] = Json(request.SystemName?.Trim()),
            [TenantSettingKeys.PosLoginDescription] = Json(request.Description?.Trim()),
            [TenantSettingKeys.PosLoginSubtitleTemplate] = Json(request.SubtitleTemplate?.Trim()),
            [TenantSettingKeys.PosLoginBackgroundMode] = Json(request.BackgroundMode?.Trim().ToUpperInvariant()),
            [TenantSettingKeys.PosLoginBackgroundColor] = Json(request.BackgroundColor?.Trim().ToUpperInvariant()),
            [TenantSettingKeys.PosLoginBackgroundMediaAssetId] = Json(request.BackgroundMediaAssetId?.ToString()),
            [TenantSettingKeys.PosLoginHeroMediaAssetId] = Json(request.HeroMediaAssetId?.ToString())
        };

        await _repository.SaveSettingsAsync(context.TenantId, values, DateTimeOffset.UtcNow, cancellationToken);
        tenant = await _repository.FindTenantAsync(context.TenantId, cancellationToken) ?? tenant;
        return ApplicationResult<TenantAdminPosLoginBrandingResponse>.Success(
            await BuildAdminResponseAsync(tenant, cancellationToken));
    }

    private async Task<TenantAdminPosLoginBrandingResponse> BuildAdminResponseAsync(
        PosLoginBrandingTenantSnapshot tenant,
        CancellationToken cancellationToken)
    {
        var values = await _repository.GetSettingValuesAsync(tenant.TenantId, cancellationToken);
        var configured = new PosLoginBrandingConfiguredDto(
            ReadString(values, TenantSettingKeys.PosLoginSystemName),
            ReadString(values, TenantSettingKeys.PosLoginDescription),
            ReadString(values, TenantSettingKeys.PosLoginSubtitleTemplate),
            ReadString(values, TenantSettingKeys.PosLoginBackgroundMode),
            ReadString(values, TenantSettingKeys.PosLoginBackgroundColor),
            ReadGuid(values, TenantSettingKeys.PosLoginBackgroundMediaAssetId),
            ReadGuid(values, TenantSettingKeys.PosLoginHeroMediaAssetId));
        return new TenantAdminPosLoginBrandingResponse(configured, await ResolveEffectiveAsync(tenant, values, cancellationToken));
    }

    private async Task<PublicPosLoginBrandingResponse> ResolveEffectiveAsync(
        PosLoginBrandingTenantSnapshot tenant,
        CancellationToken cancellationToken) =>
        await ResolveEffectiveAsync(tenant, await _repository.GetSettingValuesAsync(tenant.TenantId, cancellationToken), cancellationToken);

    private async Task<PublicPosLoginBrandingResponse> ResolveEffectiveAsync(
        PosLoginBrandingTenantSnapshot tenant,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var brand = FirstNonEmpty(tenant.TradingName, tenant.DisplayName, PosLoginBrandingDefaults.BrandName);
        var systemName = FirstNonEmpty(ReadString(values, TenantSettingKeys.PosLoginSystemName), PosLoginBrandingDefaults.SystemName);
        var description = FirstNonEmpty(ReadString(values, TenantSettingKeys.PosLoginDescription), PosLoginBrandingDefaults.Description);
        var template = FirstNonEmpty(ReadString(values, TenantSettingKeys.PosLoginSubtitleTemplate), PosLoginBrandingDefaults.SubtitleTemplate);
        if (!PosLoginBrandingValidator.HasOnlyTenantNamePlaceholder(template)) template = PosLoginBrandingDefaults.SubtitleTemplate;
        var mode = ReadString(values, TenantSettingKeys.PosLoginBackgroundMode)?.ToUpperInvariant();
        if (mode is null || !PosLoginBrandingValidator.IsBackgroundMode(mode)) mode = PosLoginBrandingDefaults.BackgroundMode;
        var color = ReadString(values, TenantSettingKeys.PosLoginBackgroundColor)?.ToUpperInvariant();
        if (color is null || !PosLoginBrandingValidator.IsColor(color)) color = PosLoginBrandingDefaults.BackgroundColor;

        var logo = tenant.LogoMediaAssetId is { } logoId ? await _repository.FindMediaAsync(logoId, cancellationToken) : null;
        var backgroundId = ReadGuid(values, TenantSettingKeys.PosLoginBackgroundMediaAssetId);
        var background = backgroundId is { } bgId ? await _repository.FindMediaAsync(bgId, cancellationToken) : null;
        var heroId = ReadGuid(values, TenantSettingKeys.PosLoginHeroMediaAssetId);
        var hero = heroId is { } hId ? await _repository.FindMediaAsync(hId, cancellationToken) : null;
        var timestamps = new[] { tenant.UpdatedAt, logo?.UpdatedAt, background?.UpdatedAt, hero?.UpdatedAt }.Where(x => x.HasValue).Select(x => x!.Value);

        return new PublicPosLoginBrandingResponse(
            tenant.TenantSlug,
            brand,
            systemName,
            description,
            template.Replace("{tenantName}", brand, StringComparison.Ordinal),
            mode,
            color,
            PosLoginBrandingValidator.IsEffectiveMedia(logo, tenant.TenantId, MediaAssetPurposes.TenantLogo) ? logo!.PublicUrl : null,
            mode == "IMAGE" && PosLoginBrandingValidator.IsEffectiveMedia(background, tenant.TenantId, MediaAssetPurposes.PosLoginBackground) ? background!.PublicUrl : null,
            PosLoginBrandingValidator.IsEffectiveMedia(hero, tenant.TenantId, MediaAssetPurposes.PosLoginHero) ? hero!.PublicUrl : null,
            timestamps.Max());
    }

    private static ApplicationError? Validate(UpdatePosLoginBrandingRequest request)
    {
        var checks = new[]
        {
            PosLoginBrandingValidator.ValidateText(request.SystemName, 80, false, "pos_login_branding.system_name_invalid", "System name"),
            PosLoginBrandingValidator.ValidateText(request.Description, 300, false, "pos_login_branding.description_invalid", "Description"),
            PosLoginBrandingValidator.ValidateText(request.SubtitleTemplate, 160, false, "pos_login_branding.subtitle_invalid", "Subtitle template")
        };
        var error = checks.FirstOrDefault(x => x is not null);
        if (error is not null) return error;
        if (request.BackgroundMode is { } mode && !PosLoginBrandingValidator.IsBackgroundMode(mode.Trim().ToUpperInvariant()))
            return new ApplicationError("pos_login_branding.background_mode_invalid", "Background mode must be IMAGE or COLOR.");
        if (request.BackgroundColor is { } color && !PosLoginBrandingValidator.IsColor(color.Trim().ToUpperInvariant()))
            return new ApplicationError("pos_login_branding.background_color_invalid", "Background color must use #RRGGBB format.");
        if (request.SubtitleTemplate is { } template && !PosLoginBrandingValidator.HasOnlyTenantNamePlaceholder(template))
            return new ApplicationError("pos_login_branding.subtitle_invalid", "Subtitle template contains an unsupported placeholder.");
        return null;
    }

    private static string? Json(string? value) => string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Serialize(value);
    private static string? ReadString(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var json)) return null;
        try { return JsonSerializer.Deserialize<string>(json); } catch (JsonException) { return null; }
    }
    private static Guid? ReadGuid(IReadOnlyDictionary<string, string> values, string key) => Guid.TryParse(ReadString(values, key), out var value) ? value : null;
    private static string FirstNonEmpty(params string?[] values) => values.First(x => !string.IsNullOrWhiteSpace(x))!.Trim();
    private static ApplicationResult<T> Failure<T>(string code, string message) => ApplicationResult<T>.Failure(new ApplicationError(code, message));
}
