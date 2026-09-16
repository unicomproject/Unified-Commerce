#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class CheckoutPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.sales.checkout.execute", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.payments.card.accept", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.payments.qr.accept", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.payments.split.accept", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.checkout.methods.cash_tile", "pos.payments.cash.accept", "Checkout", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Cash method tile chrome", true),
        new("pos.checkout.methods.card_tile", "pos.payments.card.accept", "Checkout", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Card method tile chrome", true),
        new("pos.checkout.methods.qr_tile", "pos.payments.qr.accept", "Checkout", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "QR method tile chrome", true),
        new("pos.checkout.methods.split_tile", "pos.payments.split.accept", "Checkout", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Split method tile chrome", true),
        new("pos.checkout.methods.container", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Payment methods container", true),
        new("pos.checkout.summary.payment", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Payment summary", true),
        new("pos.checkout.summary.items", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Items", true),
        new("pos.checkout.summary.quantity", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Quantity", true),
        new("pos.checkout.summary.price", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Price", true),
        new("pos.checkout.summary.line_total", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Line total", true),
        new("pos.checkout.customer.summary", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Customer summary", true),
        new("pos.checkout.summary.subtotal", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Subtotal", true),
        new("pos.checkout.summary.discount", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Discount", true),
        new("pos.checkout.summary.tax", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Tax", true),
        new("pos.checkout.summary.total", "pos.sales.checkout.execute", "Checkout", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Total", true),
    ];
}
