#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class CatalogPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.sales.catalog.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.catalog.search", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.catalog.sections.quick_products", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Quick products", true),
        new("pos.catalog.sections.popular", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Popular", true),
        new("pos.catalog.sections.frequently_sold", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Frequently sold", true),
        new("pos.catalog.sections.offers", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Offers", true),
        new("pos.catalog.sections.sort", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Sort", true),
        new("pos.catalog.product_card.image", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Card image", true),
        new("pos.catalog.product_card.name", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Card name", true),
        new("pos.catalog.product_card.regular_price", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Regular price", true),
        new("pos.catalog.product_card.sale_price", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Sale price", true),
        new("pos.catalog.product_card.discount_badge", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Discount badge", true),
        new("pos.catalog.product_card.open_details", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Open details", true),
        new("pos.catalog.product_detail.view", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Split, "Detail view", true),
        new("pos.catalog.product_detail.close", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Close detail", true),
        new("pos.catalog.product_detail.image", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Detail image", true),
        new("pos.catalog.product_detail.name", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Detail name", true),
        new("pos.catalog.product_detail.price", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Detail price", true),
        new("pos.catalog.product_detail.stock", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Detail stock", true),
        new("pos.catalog.product_detail.sku", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Detail SKU", true),
        new("pos.catalog.product_detail.description", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Description", true),
        new("pos.catalog.product_detail.variants", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Variants", true),
        new("pos.catalog.product_detail.variant_select", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Variant selection", true),
        new("pos.catalog.product_detail.available_qty", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Available quantity", true),
        new("pos.catalog.product_detail.quantity_display", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Quantity display", true),
        new("pos.catalog.product_detail.note_view", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Note view", true),
        new("pos.catalog.product_detail.note_entry", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "Note entry", true),
        new("pos.catalog.product_detail.recommendations", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Recommendations", true),
        new("pos.catalog.product_detail.cancel", "pos.sales.catalog.view", "Catalog", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Cancel detail", true),
        new("pos.catalog.search.bar", "pos.sales.catalog.search", "Catalog", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "Search bar", true),
        new("pos.catalog.search.clear", "pos.sales.catalog.search", "Catalog", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Clear search", true),
        new("pos.catalog.search.results", "pos.sales.catalog.search", "Catalog", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Search results", true),
        new("pos.catalog.search.empty_state", "pos.sales.catalog.search", "Catalog", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "Empty search state", true),
        new("pos.catalog.search.scanner_hint", "pos.sales.catalog.search", "Catalog", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "Scanner hint", true),
    ];
}
