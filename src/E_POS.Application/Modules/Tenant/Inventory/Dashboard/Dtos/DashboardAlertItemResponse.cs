namespace E_POS.Application.Modules.Tenant.Inventory.Dashboard.Dtos;

public sealed record DashboardAlertItemResponse(
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string? VariantName,
    string? Sku,
    Guid OutletId,
    string OutletName,
    string AlertType, // "LowStock", "OutOfStock", "NearExpiry"
    string Severity, // "Critical", "Warning"
    DateTimeOffset DetectedOn);
