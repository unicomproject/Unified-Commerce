namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantProfileDetailDto(
    string? LegalName,
    string? PrimaryContactName,
    string? PrimaryEmail,
    string? PrimaryPhone,
    string? WebsiteUrl);

