using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

public static class CommercialClassification
{
    public const string CoreEntitlementIndependent = "CORE_ENTITLEMENT_INDEPENDENT";
    public const string CoreAlwaysIncluded = "CORE_ALWAYS_INCLUDED";
    public const string PlanSelectable = "PLAN_SELECTABLE";
    public const string NonCommercialInfrastructure = "NON_COMMERCIAL_INFRASTRUCTURE";
    public const string ExcludedR1 = "EXCLUDED_R1";
}

public sealed record BusinessCapabilityDefinition(
    string Code,
    string Name,
    string Description,
    IReadOnlyList<string> MappedTechnicalFeatureCodes,
    string CommercialClassification = CommercialClassification.PlanSelectable);

public sealed record BusinessModuleDefinition(
    string Code,
    string Name,
    string Description,
    int DisplayOrder,
    string ReleaseCode,
    string CurrentR1Status,
    string CommercialState,
    IReadOnlyList<BusinessCapabilityDefinition> Capabilities);

public static class BusinessCapabilityCatalog
{
    public const string ReleaseCode = "R1";

    public static readonly IReadOnlyList<BusinessModuleDefinition> Modules = new List<BusinessModuleDefinition>
    {
        new(
            Code: "BM-01",
            Name: "Authentication & Workspace",
            Description: "Staff/Admin login, OTP, JWT, tenant/outlet selection context",
            DisplayOrder: 1,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "PRODUCTION READY / CLOSED",
            CommercialState: "CORE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM01.STAFF_AUTHENTICATION", "Staff Authentication", "User login with password or pin", new[] { PlatformTenantFeatureCodes.TenantProfile, PlatformTenantFeatureCodes.TenantSettings }, CommercialClassification.CoreEntitlementIndependent),
                new("BM01.SESSION_MANAGEMENT", "Session Management", "JWT token generation, refresh, and session validation", new[] { PlatformTenantFeatureCodes.TenantProfile }, CommercialClassification.CoreEntitlementIndependent),
                new("BM01.WORKSPACE_CONTEXT", "Workspace Context Resolution", "Tenant, outlet, and till context switching and scoping", new[] { PlatformTenantFeatureCodes.TenantSettings }, CommercialClassification.CoreEntitlementIndependent)
            }),

        new(
            Code: "BM-02",
            Name: "Outlet & Till Management",
            Description: "Outlets & Tills CRUD, store hierarchy setup",
            DisplayOrder: 2,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM02.OUTLET_MANAGEMENT", "Outlet Setup & Management", "Create, view, update outlets", new[] { PlatformTenantFeatureCodes.OutletManagement }),
                new("BM02.TILL_MANAGEMENT", "Till Setup & Management", "Create, view, update tills and register hardware bindings", new[] { PlatformTenantFeatureCodes.TillManagement })
            }),

        new(
            Code: "BM-03",
            Name: "Users, Roles & Permissions",
            Description: "User accounts, role definitions, permissions mapping",
            DisplayOrder: 3,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "PRODUCTION READY / CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM03.USER_ACCOUNTS", "User Account Administration", "Create, update, deactivate staff user accounts", new[] { PlatformTenantFeatureCodes.UserAccounts }),
                new("BM03.ROLE_MANAGEMENT", "Role Management & Delegation", "Define custom roles and manage role permissions", new[] { PlatformTenantFeatureCodes.RoleManagement }),
                new("BM03.PERMISSION_MANAGEMENT", "Permission Assignment", "Assign and audit granular system permissions", new[] { PlatformTenantFeatureCodes.PermissionManagement })
            }),

        new(
            Code: "BM-04",
            Name: "Devices & Hardware",
            Description: "Peripheral pairing (printers, cash drawer, barcode scanner)",
            DisplayOrder: 4,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "PARTIAL",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM04.HARDWARE_DEVICE_PAIRING", "Peripheral Device Pairing", "Pair receipt printers, cash drawers, and barcode scanners", new[] { PlatformTenantFeatureCodes.HardwareDeviceManagement }),
                new("BM04.PRINTER_DRAWER_CONFIG", "Printer & Cash Drawer Configuration", "Configure thermal printer formats and cash drawer pulse settings", new[] { PlatformTenantFeatureCodes.HardwareDeviceManagement })
            }),

        new(
            Code: "BM-05",
            Name: "Till Session & Operations",
            Description: "Open/close till session, opening float, shift control",
            DisplayOrder: 5,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM05.TILL_SESSION_OPEN_CLOSE", "Open & Close Till Session", "Start shift with opening float and perform EOD close", new[] { PlatformTenantFeatureCodes.PosCheckout }),
                new("BM05.SHIFT_CONTROL", "Shift Audit & Handover", "Track till session transactions and perform manager shift handover", new[] { PlatformTenantFeatureCodes.PosCheckout })
            }),

        new(
            Code: "BM-06",
            Name: "POS Home / Dashboard",
            Description: "Role-based POS landing, quick actions, shift status",
            DisplayOrder: 6,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM06.POS_HOME_LANDING", "POS Role-Based Landing", "Interactive POS dashboard with active session summary", new[] { PlatformTenantFeatureCodes.PosCheckout }),
                new("BM06.SHIFT_STATUS_SUMMARY", "Shift Status & Quick Actions", "Quick access to checkout, till session, and parked sales", new[] { PlatformTenantFeatureCodes.PosCheckout })
            }),

        new(
            Code: "BM-07",
            Name: "Product Catalogue Management",
            Description: "Products, variants, 7-step wizard, categories, brands, price/tax",
            DisplayOrder: 7,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM07.PRODUCT_MANAGEMENT", "Product & Variant Maintenance", "Create and manage products, variants, SKUs, and barcodes", new[] { PlatformTenantFeatureCodes.ProductCatalog }),
                new("BM07.CATEGORY_BRAND_SETUP", "Category & Brand Organization", "Organize catalog into categories and brands", new[] { PlatformTenantFeatureCodes.ProductCatalog }),
                new("BM07.PRICE_TAX_CONFIG", "Pricing & Tax Configuration", "Define selling prices and tax inclusion rules", new[] { PlatformTenantFeatureCodes.ProductCatalog })
            }),

        new(
            Code: "BM-08",
            Name: "Inventory & Stock Management",
            Description: "Stock view, receiving, adjustments, channel allocation",
            DisplayOrder: 8,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "PARTIAL",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM08.STOCK_VIEW", "Multi-Outlet Stock Visibility", "Real-time stock level lookup across outlets", new[] { PlatformTenantFeatureCodes.InventoryTracking }),
                new("BM08.STOCK_ADJUSTMENT_RECEIVING", "Stock Adjustment & Receiving", "Process stock transfers, adjustments, and purchase receipts", new[] { PlatformTenantFeatureCodes.InventoryTracking })
            }),

        new(
            Code: "BM-09",
            Name: "Sales / New Sale & Cart",
            Description: "POS catalog search, scan, cart management, line discounts",
            DisplayOrder: 9,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM09.POS_CART_MANAGEMENT", "POS Cart Management", "Product barcode scanning, cart item modification, and quantity adjustments", new[] { PlatformTenantFeatureCodes.PosCheckout }),
                new("BM09.LINE_DISCOUNT_SEARCH", "Line Items & Price Overrides", "Apply line-item adjustments and quick item search", new[] { PlatformTenantFeatureCodes.PosCheckout })
            }),

        new(
            Code: "BM-10",
            Name: "Customer Management",
            Description: "Customer search, creation, attachment to sale, history",
            DisplayOrder: 10,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM10.CUSTOMER_PROFILE", "Customer Directory & Profiles", "Create and maintain customer contact information", new[] { PlatformTenantFeatureCodes.PosCheckout }),
                new("BM10.CUSTOMER_ATTACH_SALE", "Customer Sale Linking", "Search and attach customer profile to active POS checkout cart", new[] { PlatformTenantFeatureCodes.PosCheckout })
            }),

        new(
            Code: "BM-11",
            Name: "Park & Recall Sales",
            Description: "Hold transaction, list parked sales, recall to active cart",
            DisplayOrder: 11,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM11.PARK_SALE", "Hold Transaction", "Park active shopping cart with custom note", new[] { PlatformTenantFeatureCodes.PosCheckout }),
                new("BM11.RECALL_SALE", "Recall Parked Transaction", "Retrieve parked transactions back to active POS cart", new[] { PlatformTenantFeatureCodes.PosCheckout })
            }),

        new(
            Code: "BM-12",
            Name: "Payments",
            Description: "POS Cash/Card/LankaQR payments, split payments",
            DisplayOrder: 12,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM12.POS_PAYMENTS", "POS Payment Processing", "Accept cash, card, and LankaQR payments at POS checkout", new[] { PlatformTenantFeatureCodes.PosCheckout }),
                new("BM12.ONLINE_PAYMENTS", "E-Commerce Gateway Integration", "Process online storefront credit card payments", new[] { PlatformTenantFeatureCodes.OnlineStore })
            }),

        new(
            Code: "BM-13",
            Name: "Receipts",
            Description: "Receipt generation, local thermal print, reprint, digital receipt",
            DisplayOrder: 13,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM13.RECEIPT_GENERATION_PRINT", "Receipt Printing & Reprinting", "Generate formatted thermal receipt and perform manager reprints", new[] { PlatformTenantFeatureCodes.PosCheckout })
            }),

        new(
            Code: "BM-14",
            Name: "Returns, Refunds & Exchanges",
            Description: "Process order return, cash/card refund, item exchange",
            DisplayOrder: 14,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM14.ORDER_RETURN_REFUND", "Order Return & Refund Processing", "Lookup completed orders, process item returns, and issue refunds", new[] { PlatformTenantFeatureCodes.PosCheckout }),
                new("BM14.ITEM_EXCHANGE", "Even & Uneven Item Exchanges", "Swap returned items with new items in a single transaction", new[] { PlatformTenantFeatureCodes.PosCheckout })
            }),

        new(
            Code: "BM-15",
            Name: "Cash Management & Till Reconciliation",
            Description: "Cash in/out drops, denomination count, variance, EOD close",
            DisplayOrder: 15,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM15.CASH_DROPS", "Cash In / Cash Out Drops", "Record petty cash movements and bank safe drops", new[] { PlatformTenantFeatureCodes.PosCheckout }),
                new("BM15.EOD_RECONCILIATION", "End-Of-Day Till Reconciliation", "Perform currency denomination count and calculate cash variance", new[] { PlatformTenantFeatureCodes.PosCheckout })
            }),

        new(
            Code: "BM-16",
            Name: "Online Orders & Click & Collect",
            Description: "Storefront browsing, cart, online checkout, pick/prepare, pickup",
            DisplayOrder: 16,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "IMPLEMENTED — NOT YET E2E CLOSED",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM16.STOREFRONT_ONLINE_STORE", "Online Storefront Catalog & Cart", "Customer digital storefront catalog, cart, and checkout", new[] { PlatformTenantFeatureCodes.OnlineStore }),
                new("BM16.ONLINE_ORDERS", "Online Order Management", "Receive and process incoming e-commerce web orders", new[] { PlatformTenantFeatureCodes.SalesOrders }),
                new("BM16.CLICK_COLLECT_PICKUP", "Click & Collect In-Store Pickup", "Notify customer and verify in-store pickup fulfillment", new[] { PlatformTenantFeatureCodes.ClickCollect })
            }),

        new(
            Code: "BM-17",
            Name: "Reporting & Analytics",
            Description: "Sales, inventory, tax, cashier EOD reports, exports",
            DisplayOrder: 17,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "PARTIAL",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM17.SALES_REPORTS", "Sales & Revenue Analytics", "Generate daily sales summaries, outlet performance, and cashier audit reports", new[] { PlatformTenantFeatureCodes.SalesReports }),
                new("BM17.ANALYTICS_EXPORT", "Report Exporting & Audit", "Export analytics data to PDF/CSV for accounting reconciliation", new[] { PlatformTenantFeatureCodes.SalesReports })
            }),

        new(
            Code: "BM-18",
            Name: "Offline & Synchronization",
            Description: "Local SQLite outbox, offline transaction queueing, auto sync",
            DisplayOrder: 18,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "PARTIAL",
            CommercialState: "SELECTABLE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM18.LOCAL_OUTBOX_QUEUE", "Offline Outbox Queueing", "Queue transactions locally during internet disconnect", new[] { PlatformTenantFeatureCodes.OfflineOperationSync }),
                new("BM18.OFFLINE_SYNC", "Automatic Synchronization", "Background sync of queued offline sales once connection restores", new[] { PlatformTenantFeatureCodes.OfflineOperationSync })
            }),

        new(
            Code: "BM-19",
            Name: "Business / POS Settings",
            Description: "Receipt configuration, payment options, device preferences",
            DisplayOrder: 19,
            ReleaseCode: ReleaseCode,
            CurrentR1Status: "PARTIAL",
            CommercialState: "CORE",
            Capabilities: new List<BusinessCapabilityDefinition>
            {
                new("BM19.TENANT_BUSINESS_SETTINGS", "Tenant General Settings", "Store name, currency format, tax registration numbers, and receipt header", new[] { PlatformTenantFeatureCodes.TenantSettings }, CommercialClassification.CoreAlwaysIncluded),
                new("BM19.POS_PREFERENCES", "POS Preference Configuration", "Configure default payment methods, tax behavior, and till rules", new[] { PlatformTenantFeatureCodes.TenantSettings }, CommercialClassification.CoreAlwaysIncluded)
            })
    };

    public static readonly IReadOnlyList<BusinessCapabilityDefinition> R1ExcludedCapabilities = new List<BusinessCapabilityDefinition>
    {
        new("BM_EXCLUDED.DISCOUNTS_OFFERS", "Discounts & Offers", "Cart-level and item promo discounts (Excluded from R1)", Array.Empty<string>(), CommercialClassification.ExcludedR1),
        new("BM_EXCLUDED.PROMOTIONS", "Campaign Promotions", "Automated marketing promotional rules (Excluded from R1)", Array.Empty<string>(), CommercialClassification.ExcludedR1),
        new("BM_EXCLUDED.LOYALTY", "Loyalty Program", "Customer loyalty rewards program (Excluded from R1)", Array.Empty<string>(), CommercialClassification.ExcludedR1),
        new("BM_EXCLUDED.MEMBERSHIPS", "Customer Memberships", "Tiered membership management (Excluded from R1)", Array.Empty<string>(), CommercialClassification.ExcludedR1),
        new("BM_EXCLUDED.POINTS_REWARDS", "Points & Benefits", "Earn and redeem reward points (Excluded from R1)", Array.Empty<string>(), CommercialClassification.ExcludedR1)
    };
}
