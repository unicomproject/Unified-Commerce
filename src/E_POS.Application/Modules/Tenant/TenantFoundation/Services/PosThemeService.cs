using System.Text.Json;
using System.Text.RegularExpressions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Services;

public sealed partial class PosThemeService : IPosThemeService
{
    public const string DefaultPrimaryColor = "#FF6A00";
    public const string DefaultSecondaryColor = "#000000";

    private readonly IPosLoginBrandingRepository _repository;

    public PosThemeService(IPosLoginBrandingRepository repository) =>
        _repository = repository;

    public async Task<ApplicationResult<PosThemeDto>> GetAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var tenant = await _repository.FindTenantAsync(context.TenantId, cancellationToken);
        if (tenant is null)
        {
            return ApplicationResult<PosThemeDto>.Failure(
                new ApplicationError("pos_theme.tenant_unavailable", "Tenant theme is unavailable."));
        }

        var values = await _repository.GetResolvedSettingValuesAsync(
            context.TenantId,
            [TenantSettingKeys.PosThemePrimaryColor, TenantSettingKeys.PosThemeSecondaryColor],
            cancellationToken);
        return ApplicationResult<PosThemeDto>.Success(new PosThemeDto(
            Resolve(values, TenantSettingKeys.PosThemePrimaryColor, DefaultPrimaryColor),
            Resolve(values, TenantSettingKeys.PosThemeSecondaryColor, DefaultSecondaryColor)));
    }

    private static string Resolve(
        IReadOnlyDictionary<string, string> values,
        string key,
        string fallback)
    {
        if (!values.TryGetValue(key, out var raw)) return fallback;

        string? value;
        try
        {
            value = JsonSerializer.Deserialize<string>(raw);
        }
        catch (JsonException)
        {
            value = raw;
        }

        value = value?.Trim();
        return value is not null && HexColorRegex().IsMatch(value)
            ? value.ToUpperInvariant()
            : fallback;
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();
}
