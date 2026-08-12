namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantBootstrapTenantSummaryDto(
    Guid Id,
    string Name,
    string Code,
    string LifecycleStatus,
    string? PlanName);

public sealed record PlatformTenantBootstrapModuleStatusDto(
    string ModuleKey,
    string Status,
    int Count,
    bool Entitled,
    bool CanConfigure,
    string? DependencyNotice);

public sealed record PlatformTenantBootstrapSummaryResponse(
    PlatformTenantBootstrapTenantSummaryDto Tenant,
    IReadOnlyList<PlatformTenantBootstrapModuleStatusDto> Modules);

public sealed class PlatformTenantBootstrapOutletCreateRequest
{
    public string OutletName { get; set; } = string.Empty;
    public string OutletType { get; set; } = "STORE";
    public string Timezone { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public PlatformTenantBootstrapOutletAddressRequest Address { get; set; } = new();
}

public sealed class PlatformTenantBootstrapOutletAddressRequest
{
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? StateOrProvince { get; set; }
}

public sealed record PlatformTenantBootstrapOutletResponse(
    Guid OutletId,
    string OutletName,
    string OutletCode,
    string OutletType,
    string Status,
    string Timezone);

public sealed class PlatformTenantBootstrapTillCreateRequest
{
    public Guid OutletId { get; set; }
    public string TillName { get; set; } = string.Empty;
    public string TillCode { get; set; } = string.Empty;
}

public sealed record PlatformTenantBootstrapTillResponse(
    Guid TillId,
    string TillName,
    string TillCode,
    Guid OutletId,
    string Status,
    string DeviceBindingStatus);

public sealed class PlatformTenantBootstrapRoleCreateRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<string> PermissionCodes { get; set; } = [];
}

public sealed record PlatformTenantBootstrapRoleResponse(
    Guid RoleId,
    string RoleName,
    string RoleCode,
    IReadOnlyList<string> PermissionCodes);

public sealed class PlatformTenantBootstrapUserCreateRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid RoleId { get; set; }
    public IReadOnlyList<Guid>? OutletIds { get; set; }
}

public sealed record PlatformTenantBootstrapUserResponse(
    Guid UserId,
    string DisplayName,
    string Email,
    string Status,
    string InviteStatus);

public sealed class PlatformTenantBootstrapProductCreateRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Barcode { get; set; }
    public bool? TrackInventory { get; set; }
    public decimal? OpeningStockQuantity { get; set; }
    public Guid? OutletId { get; set; }
    public string? Status { get; set; }
}

public sealed record PlatformTenantBootstrapProductResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    string Status);

public sealed record PlatformTenantBootstrapProductImportValidateResponse(
    Guid ImportId,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<PlatformTenantBootstrapProductImportPreviewInvalidRow> PreviewInvalidRows);

public sealed record PlatformTenantBootstrapProductImportPreviewInvalidRow(
    int RowNumber,
    string ErrorCode,
    string ErrorDetail);

public sealed record PlatformTenantBootstrapProductImportCommitResponse(
    Guid ImportId,
    int CommittedRows,
    int SkippedRows);

public sealed class PlatformTenantBootstrapOnlineStoreUpsertRequest
{
    public string StoreStatus { get; set; } = string.Empty;
    public string? TaxDisplayMode { get; set; }
}

public sealed record PlatformTenantBootstrapOnlineStoreResponse(
    bool Entitled,
    string StoreStatus,
    string TaxDisplayMode,
    bool ClickCollectEntitled,
    bool ClickCollectConfigured,
    string? DependencyNotice);
