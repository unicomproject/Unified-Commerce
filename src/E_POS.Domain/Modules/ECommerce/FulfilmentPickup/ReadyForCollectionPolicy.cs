using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup;

/// <summary>Read-only eligibility; never performs the upstream Ready transition.</summary>
public static class ReadyForCollectionPolicy
{
    public static bool IsReady(SalesOrder order, FulfillmentOrder fulfillment, PickupOrder? pickup) =>
        fulfillment.FulfillmentStatus == "READY" &&
        fulfillment.ReadyAt.HasValue &&
        fulfillment.CancelledAt is null && fulfillment.FulfilledAt is null &&
        pickup is { PickupStatus: "READY", CollectedAt: null } &&
        order.Status is not ("CANCELLED" or "COMPLETED" or "COLLECTED" or "FULFILLED" or "VOIDED") &&
        order.FulfillmentStatus is not ("CANCELLED" or "COLLECTED" or "FULFILLED") &&
        order.CancelledAt is null && order.CompletedAt is null;
}
