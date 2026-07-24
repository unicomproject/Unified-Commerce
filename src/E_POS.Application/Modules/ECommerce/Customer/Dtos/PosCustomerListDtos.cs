namespace E_POS.Application.Modules.ECommerce.Customer.Dtos;

public sealed record PosCustomerCreateRequestDto(
    string FullName,
    string Phone,
    string? Email);

public sealed record PosCustomerUpdateRequestDto(
    string FullName,
    string Phone,
    string? Email,
    string Status);

public sealed record PosCustomerListItemResponseDto(
    Guid CustomerId,
    string FullName,
    string? Phone,
    string? Email,
    string Status,
    string? CustomerCode = null,
    string? SourceType = null,
    DateTimeOffset? JoinedAt = null,
    int TotalOrderCount = 0,
    decimal TotalSpentAmount = 0,
    string? CurrencyCode = null,
    DateTimeOffset? LastPurchaseAt = null,
    bool IsMixedCurrencySpend = false);

public sealed record PosCustomerListResponseDto(
    IReadOnlyList<PosCustomerListItemResponseDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PosCustomerSummaryResponseDto(
    int TotalCustomers,
    int ActiveCustomers,
    int CustomersWithOrders,
    int NewCustomersThisMonth,
    string TimeZoneId);

public sealed record PosCustomerOrderItemDto(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset OrderDate,
    decimal TotalAmount,
    string CurrencyCode,
    string Status,
    string? OutletDisplayName);

public sealed record PosCustomerOrdersResponseDto(
    IReadOnlyList<PosCustomerOrderItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PosCustomerAttachToSaleRequestDto(
    Guid? SaleId);

public sealed record PosCustomerAttachToSaleResponseDto(
    Guid CustomerId,
    string FullName,
    string? Phone,
    string? Email,
    string Status,
    string? CustomerCode,
    Guid? SaleId,
    string AttachmentMode,
    bool IsActive);
