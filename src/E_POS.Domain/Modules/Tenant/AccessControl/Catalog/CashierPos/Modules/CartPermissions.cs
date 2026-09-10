#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class CartPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.sales.new_sale.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.new_sale.create", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.cart.manage", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.cart.add_item", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.cart.update_item", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.cart.remove_item", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.cart.clear", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.cart.summary.view", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Cart summary container", true),
        new("pos.cart.summary.item_count", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Cart item count", true),
        new("pos.cart.summary.subtotal", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Cart subtotal", true),
        new("pos.cart.summary.discount", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Cart discount", true),
        new("pos.cart.summary.tax", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Cart tax", true),
        new("pos.cart.summary.total", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Cart total", true),
        new("pos.cart.lines.list", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Cart lines list", true),
        new("pos.cart.lines.name", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Line product name", true),
        new("pos.cart.lines.quantity", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Line quantity display", true),
        new("pos.cart.lines.unit_price", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Line unit price", true),
        new("pos.cart.lines.line_total", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Line total", true),
        new("pos.cart.lines.note", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Line note display", true),
        new("pos.cart.lines.image", "pos.sales.cart.manage", "Cart", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Line product image", true),
        new("pos.new_sale.chrome.header", "pos.sales.new_sale.view", "NewSale", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "New sale header", true),
        new("pos.new_sale.chrome.park_action", "pos.sales.new_sale.view", "NewSale", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Park action chrome", true),
        new("pos.new_sale.chrome.customer_chip", "pos.sales.new_sale.view", "NewSale", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Customer chip", true),
        new("pos.new_sale.chrome.empty_cart", "pos.sales.new_sale.view", "NewSale", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "Empty cart message", true),
        new("pos.new_sale.chrome.checkout_action", "pos.sales.new_sale.view", "NewSale", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Proceed to checkout chrome", true),
        new("pos.new_sale.chrome.held_count", "pos.sales.new_sale.view", "NewSale", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Held sales count badge", true),
        new("pos.new_sale.chrome.clear_cart_action", "pos.sales.new_sale.view", "NewSale", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Clear cart chrome", true),
    ];
}
