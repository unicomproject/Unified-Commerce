namespace E_POS.Application.Modules.Tenant.Reports.Dtos;

public sealed record ReportQueryRequest(
    DateOnly? From,
    DateOnly? To,
    Guid? OutletId,
    Guid? TillId,
    Guid? CashierId,
    Guid? CustomerId,
    Guid? DepartmentId,
    Guid? CategoryId,
    Guid? SubcategoryId,
    Guid? BrandId,
    Guid? ProductId,
    Guid? ProductVariantId,
    Guid? SalesChannelId,
    Guid? PaymentMethodId,
    string? OrderStatus,
    string? PaymentStatus,
    string? Search,
    string? Section,
    int Page = 1,
    int PageSize = 25,
    string? SortBy = null,
    string? SortDirection = "asc",
    Guid? InventoryLocationId = null,
    string? StockStatus = null,
    string? ExpiryStatus = null,
    string? BatchNumber = null,
    string? MovementType = null);

public sealed record ReportFilterOptionsRequest(
    Guid? OutletId,
    Guid? DepartmentId,
    Guid? CategoryId,
    Guid? ProductId,
    string? Search,
    string? OptionType,
    bool IncludeInactive = false,
    int Page = 1,
    int PageSize = 25);

public sealed record ReportFilterOptionDto(
    string? Id,
    string? Code,
    string Name,
    string? Status,
    string? ParentId,
    string? SecondaryLabel,
    bool IsActive);

public sealed record ReportFilterOptionsResponse(
    DateOnly BusinessDate,
    string Timezone,
    string CurrencyCode,
    string Locale,
    IReadOnlyDictionary<string, IReadOnlyList<ReportFilterOptionDto>> Groups);

public sealed record ReportPageDto(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ReportResultDto(
    string Section,
    string CurrencyCode,
    string Timezone,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyDictionary<string, object?> Summary,
    IReadOnlyDictionary<string, object?> Sections,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Records,
    ReportPageDto? Pagination,
    DateTimeOffset GeneratedAt);

public sealed record SalesTransactionDetailDto(
    Guid OrderId,
    string OrderNumber,
    IReadOnlyDictionary<string, object?> InvoiceInformation,
    IReadOnlyDictionary<string, object?> FinancialSummary,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Items,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Payments,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Discounts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Taxes,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ReturnsAndRefunds,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Notes,
    string CurrencyCode);

public sealed record ReportExportRequest(
    string ReportType,
    string Section,
    string Format,
    ReportQueryRequest Filters);

public sealed record ReportExportDto(
    Guid JobId,
    string ReportType,
    string Section,
    string Format,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    string? FileName,
    string? DownloadUrl,
    DateTimeOffset? ExpiresAt,
    string? ErrorMessage);
