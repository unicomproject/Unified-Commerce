namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformSettingsResponse
{
    public string? PlatformDisplayName { get; init; }

    public string? SupportEmail { get; init; }

    public string? DefaultCountryCode { get; init; }

    public string? DefaultCurrencyCode { get; init; }

    public string? DefaultTimezone { get; init; }

    public string? DefaultLocale { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public Guid? UpdatedByPlatformUserId { get; init; }
}

public sealed record UpdatePlatformSettingsRequest
{
    public string? PlatformDisplayName { get; init; }

    public string? SupportEmail { get; init; }

    public string? DefaultCountryCode { get; init; }

    public string? DefaultCurrencyCode { get; init; }

    public string? DefaultTimezone { get; init; }

    public string? DefaultLocale { get; init; }
}
