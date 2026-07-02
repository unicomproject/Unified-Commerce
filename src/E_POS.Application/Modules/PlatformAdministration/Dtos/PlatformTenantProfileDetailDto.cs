namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformTenantProfileDetailDto(
    string? LegalName,
    string? PrimaryContactName,
    string? PrimaryEmail,
    string? PrimaryPhone,
    string? WebsiteUrl);
