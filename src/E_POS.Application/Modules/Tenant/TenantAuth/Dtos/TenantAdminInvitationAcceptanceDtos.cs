namespace E_POS.Application.Modules.Tenant.TenantAuth.Dtos;

public sealed record ValidateTenantAdminSetupTokenResponse(
    string SetupToken,
    bool Valid,
    bool Expired,
    string? Email,
    string? Message);

public sealed record SetupTenantAdminPasswordRequest(
    string SetupToken,
    string Password,
    string ConfirmPassword);

public sealed record SetupTenantAdminPasswordResponse(
    bool Success,
    string Message);
