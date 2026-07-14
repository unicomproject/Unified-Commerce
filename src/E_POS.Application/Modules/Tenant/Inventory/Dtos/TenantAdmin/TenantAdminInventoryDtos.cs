namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;

public sealed record TenantAdminCurrentStockQuery(
    Guid? OutletId,
    string? Search,
    string? StockStatus,
    Guid? CategoryId,
    string? BatchNumber,
    string? ExpiryStatus,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection);

public sealed record TenantAdminCurrentStockVariantOptionResponse(
    string Name,
    string Value);

public sealed record TenantAdminCurrentStockListItemResponse(
    Guid InventoryBalanceId,
    Guid InventoryLocationId,
    Guid OutletId,
    string OutletName,
    Guid ProductId,
    string ProductName,
    Guid? ProductVariantId,
    string? VariantName,
    IReadOnlyList<TenantAdminCurrentStockVariantOptionResponse> VariantOptions,
    string? Sku,
    string? Barcode,
    Guid? ProductBatchId,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal DamagedQuantity,
    decimal QuarantineQuantity,
    decimal AvailableQuantity,
    string StockStatus,
    string ExpiryStatus,
    DateTimeOffset? LastMovementAt,
    long RowVersion);

public sealed record TenantAdminCurrentStockListResponse(
    IReadOnlyList<TenantAdminCurrentStockListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record TenantAdminCurrentStockSummaryResponse(
    int TotalProducts,
    int TotalVariants,
    decimal TotalUnits,
    int LowStockCount,
    int OutOfStockCount,
    int ExpiringSoonCount);

public sealed class TenantAdminStockInLineRequest
{
    public Guid ProductVariantId { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ManufacturedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Barcode { get; set; }
}

public sealed class TenantAdminStockInRequest
{
    public Guid OutletId { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public string? IdempotencyKey { get; set; }
    public IReadOnlyList<TenantAdminStockInLineRequest> Items { get; set; } = [];
}

public sealed record TenantAdminStockInLineResponse(
    Guid ProductVariantId,
    string VariantName,
    Guid? ProductBatchId,
    string? BatchNumber,
    decimal QuantityReceived,
    decimal OnHandAfter,
    decimal AvailableAfter,
    Guid StockMovementId);

public sealed record TenantAdminStockInResponse(
    Guid OperationId,
    Guid OutletId,
    string OutletName,
    string? ReferenceNumber,
    DateTimeOffset ReceivedAt,
    int ItemCount,
    decimal TotalQuantity,
    string Status,
    IReadOnlyList<TenantAdminStockInLineResponse> Items,
    DateTimeOffset CreatedAt);

public sealed record TenantAdminInventoryVariantOptionValueResponse(
    string AttributeName,
    string Value);

public sealed record TenantAdminInventoryVariantLookupItemResponse(
    Guid Id,
    string Name,
    string? Sku,
    string? Barcode,
    string Status,
    bool IsBatchTracked,
    bool IsExpiryTracked,
    IReadOnlyList<TenantAdminInventoryVariantOptionValueResponse> OptionValues);

public sealed record TenantAdminInventoryVariantLookupResponse(
    Guid ProductId,
    string ProductName,
    bool IsBatchTracked,
    bool IsExpiryTracked,
    IReadOnlyList<TenantAdminInventoryVariantLookupItemResponse> Variants);

public sealed record TenantAdminInventoryTrackingProfile(
    Guid ProductId,
    Guid VariantId,
    string VariantName,
    bool IsStockTracked,
    bool RequiresBatchTracking,
    bool RequiresExpiryTracking);
