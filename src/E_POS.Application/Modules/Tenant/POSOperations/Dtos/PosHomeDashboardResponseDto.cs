namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record PosHomeDashboardResponseDto(
    bool ContextResolved,
    string? ReasonCode,
    string? Message,
    string? RequiredAction,

    // Legacy Flutter keys (populated when ContextResolved is true).
    PosHomeUserDto? User,
    PosHomeContextDto? Context,
    IReadOnlyCollection<string>? Permissions,
    int UnreadNotificationCount,
    PosHomeCardsDto? Cards,

    // Structured POS home contract.
    PosHomeCashierDto? Cashier,
    PosHomeDeviceDto? Device,
    PosHomeTillDto? Till,
    PosHomeTimeDto? Time,
    PosHomeNotificationsDto? Notifications,
    PosHomeMetricsDto? Metrics,
    PosHomeQuickActionsDto? QuickActions,
    PosHomeBrandingDto? Branding = null,
    PosHomeSummaryDto? Summary = null);

public sealed record PosHomeBrandingDto(
    string DisplayName,
    string? LogoUrl);

public sealed record PosHomeUserDto(
    string FullName);

public sealed record PosHomeContextDto(
    string OutletName,
    string TillName,
    string TillSessionId);

public sealed record PosHomeCardsDto(
    PosHomeCardDto StartSale,
    PosHomeCardDto? OnlineOrders,
    PosHomeCardDto ReturnsRefunds,
    PosHomeCardDto Customers,
    PosHomeCardDto ParkedSales,
    PosHomeCardDto CashDrawer);

public sealed record PosHomeCardDto(
    bool Enabled,
    int? Count = null,
    int? NewThisWeekCount = null,
    int? OlderThanThirtyMinutesCount = null,
    double? Balance = null);

public sealed record PosHomeCashierDto(
    Guid Id,
    string DisplayName,
    string RoleLabel = "");

public sealed record PosHomeDeviceDto(
    Guid Id,
    string Code,
    string Name,
    bool Trusted,
    string Status);

public sealed record PosHomeTillDto(
    Guid Id,
    string Code,
    string Name,
    string AreaName,
    int Number,
    string Status,
    Guid? SessionId,
    DateOnly? BusinessDate,
    string CurrencyCode,
    string DisplayLabel);

public sealed record PosHomeNotificationsDto(
    int UnreadCount);

public sealed record PosHomeMetricsDto(
    int ParkedSalesCount);

public sealed record PosHomeSummaryDto(
    string Scope,
    DateOnly BusinessDate,
    Guid TillSessionId,
    string CurrencyCode,
    decimal GrossSalesAmount,
    int TransactionCount,
    decimal RefundAmount,
    int RefundCount,
    decimal DiscountAmount,
    decimal NetSalesAmount);

public sealed record PosHomeQuickActionsDto(
    bool CanStartSale,
    bool CanAddCustomer,
    bool CanViewReturns,
    bool CanViewParkedSales,
    bool CanViewCashDrawer,
    bool CanViewNotifications);
