using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformDashboardRepository
{
    Task<PlatformDashboardComputationSnapshot> GetComputationSnapshotAsync(
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken);
}

public sealed record PlatformDashboardComputationSnapshot(
    DateTimeOffset GeneratedAt,
    string? PlatformTimezone,
    IReadOnlyList<PlatformDashboardTenantRow> Tenants,
    IReadOnlyList<PlatformDashboardSubscriptionRow> Subscriptions,
    IReadOnlyList<PlatformDashboardAddonRow> Addons,
    IReadOnlyList<PlatformDashboardSubscriptionHistoryRow> SubscriptionHistory,
    IReadOnlyDictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata> Currencies,
    int PendingBillingCount,
    int TotalOutlets,
    int TotalTills,
    int TotalTenantUsers,
    int TotalPlatformUsers,
    IReadOnlyList<(DateTimeOffset CreatedAt, Guid Id)> TenantCreatedEvents,
    IReadOnlyList<(DateTimeOffset CreatedAt, Guid Id)> SubscriptionCreatedEvents);

public sealed record PlatformDashboardTenantRow(
    Guid Id,
    string TenantCode,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record PlatformDashboardSubscriptionRow(
    Guid Id,
    Guid TenantId,
    string SubscriptionStatus,
    string CurrencyCode,
    decimal PlanPrice,
    string BillingCycle,
    string? PlanBillingInterval,
    string? DiscountType,
    decimal? DiscountValue,
    DateTimeOffset CreatedAt,
    DateTimeOffset StartedAt);

public sealed record PlatformDashboardAddonRow(
    Guid TenantSubscriptionId,
    string Status,
    decimal UnitPrice,
    int Quantity,
    string CurrencyCode,
    bool AutoRenew,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt);

public sealed record PlatformDashboardSubscriptionHistoryRow(
    Guid TenantSubscriptionId,
    string ChangeType,
    DateTimeOffset ChangedAt,
    string? OldStatus,
    string? NewStatus,
    string? ChangeData);
