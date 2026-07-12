using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Customer.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;

using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Shared.Refund.Entities;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using E_POS.Domain.Modules.Shared.Notification.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence;

public sealed class EPosDbContext : DbContext
{
    public EPosDbContext(DbContextOptions<EPosDbContext> options)
        : base(options)
    {
    }

    // Platform Administration
    public DbSet<PlatformAuthSession> PlatformAuthSessions => Set<PlatformAuthSession>();
    public DbSet<PlatformLoginAudit> PlatformLoginAudits => Set<PlatformLoginAudit>();
    public DbSet<PlatformPasswordResetToken> PlatformPasswordResetTokens => Set<PlatformPasswordResetToken>();
    public DbSet<PlatformPermission> PlatformPermissions => Set<PlatformPermission>();
    public DbSet<PlatformRefreshToken> PlatformRefreshTokens => Set<PlatformRefreshToken>();
    public DbSet<PlatformRole> PlatformRoles => Set<PlatformRole>();
    public DbSet<PlatformRolePermission> PlatformRolePermissions => Set<PlatformRolePermission>();
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<PlatformUserPermission> PlatformUserPermissions => Set<PlatformUserPermission>();
    public DbSet<PlatformUserRole> PlatformUserRoles => Set<PlatformUserRole>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<PlatformSalesChannel> PlatformSalesChannels => Set<PlatformSalesChannel>();

    // Tenant Foundation
    public DbSet<BusinessType> BusinessTypes => Set<BusinessType>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<SettingDefinition> SettingDefinitions => Set<SettingDefinition>();
    public DbSet<SalesChannel> SalesChannels => Set<SalesChannel>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantAddress> TenantAddresses => Set<TenantAddress>();
    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();
    public DbSet<TenantProfile> TenantProfiles => Set<TenantProfile>();
    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();

    // Subscription Billing
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<FeatureLimitDefinition> FeatureLimitDefinitions => Set<FeatureLimitDefinition>();
    public DbSet<PlatformFeature> PlatformFeatures => Set<PlatformFeature>();
    public DbSet<PlatformModule> PlatformModules => Set<PlatformModule>();
    public DbSet<SubscriptionAddon> SubscriptionAddons => Set<SubscriptionAddon>();
    public DbSet<SubscriptionAddonFeature> SubscriptionAddonFeatures => Set<SubscriptionAddonFeature>();
    public DbSet<SubscriptionAddonLimit> SubscriptionAddonLimits => Set<SubscriptionAddonLimit>();
    public DbSet<SubscriptionCreditNote> SubscriptionCreditNotes => Set<SubscriptionCreditNote>();
    public DbSet<SubscriptionCreditNoteLine> SubscriptionCreditNoteLines => Set<SubscriptionCreditNoteLine>();
    public DbSet<SubscriptionInvoice> SubscriptionInvoices => Set<SubscriptionInvoice>();
    public DbSet<SubscriptionInvoiceLine> SubscriptionInvoiceLines => Set<SubscriptionInvoiceLine>();
    public DbSet<SubscriptionPaymentLink> SubscriptionPaymentLinks => Set<SubscriptionPaymentLink>();
    public DbSet<SubscriptionPaymentTransaction> SubscriptionPaymentTransactions => Set<SubscriptionPaymentTransaction>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<SubscriptionPlanAddon> SubscriptionPlanAddons => Set<SubscriptionPlanAddon>();
    public DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures => Set<SubscriptionPlanFeature>();
    public DbSet<SubscriptionPlanFeatureLimit> SubscriptionPlanFeatureLimits => Set<SubscriptionPlanFeatureLimit>();
    public DbSet<TenantFeatureEntitlement> TenantFeatureEntitlements => Set<TenantFeatureEntitlement>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<TenantSubscriptionAddon> TenantSubscriptionAddons => Set<TenantSubscriptionAddon>();
    public DbSet<TenantSubscriptionHistory> TenantSubscriptionHistory => Set<TenantSubscriptionHistory>();
    public DbSet<TenantUsageCounter> TenantUsageCounters => Set<TenantUsageCounter>();

    // Access Control
    public DbSet<OutletUserPermission> OutletUserPermissions => Set<OutletUserPermission>();
    public DbSet<OutletUserRole> OutletUserRoles => Set<OutletUserRole>();
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();
    public DbSet<RoleTemplate> RoleTemplates => Set<RoleTemplate>();
    public DbSet<RoleTemplateVersion> RoleTemplateVersions => Set<RoleTemplateVersion>();
    public DbSet<RoleTemplateVersionPermission> RoleTemplateVersionPermissions => Set<RoleTemplateVersionPermission>();
    public DbSet<TenantRole> TenantRoles => Set<TenantRole>();
    public DbSet<TenantRolePermission> TenantRolePermissions => Set<TenantRolePermission>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<TenantUserPermission> TenantUserPermissions => Set<TenantUserPermission>();
    public DbSet<TenantUserRole> TenantUserRoles => Set<TenantUserRole>();

    // Authentication and Security
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<TenantAuthSession> TenantAuthSessions => Set<TenantAuthSession>();
    public DbSet<TenantLoginAudit> TenantLoginAudits => Set<TenantLoginAudit>();
    public DbSet<TenantRefreshToken> TenantRefreshTokens => Set<TenantRefreshToken>();
    public DbSet<UserInvite> UserInvites => Set<UserInvite>();
    public DbSet<UserSetupToken> UserSetupTokens => Set<UserSetupToken>();

    // Outlet, Till and Device
    public DbSet<HardwareProfile> HardwareProfiles => Set<HardwareProfile>();
    public DbSet<Outlet> Outlets => Set<Outlet>();
    public DbSet<OutletAddress> OutletAddresses => Set<OutletAddress>();
    public DbSet<OutletBusinessHour> OutletBusinessHours => Set<OutletBusinessHour>();
    public DbSet<PosDevice> PosDevices => Set<PosDevice>();
    public DbSet<Till> Tills => Set<Till>();
    public DbSet<TillActivationCode> TillActivationCodes => Set<TillActivationCode>();
    public DbSet<TillDeviceAssignment> TillDeviceAssignments => Set<TillDeviceAssignment>();

    // Hardware and Cash Control
    public DbSet<CashCountDenomination> CashCountDenominations => Set<CashCountDenomination>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<CashMovementType> CashMovementTypes => Set<CashMovementType>();
    public DbSet<CashReconciliation> CashReconciliations => Set<CashReconciliation>();
    public DbSet<HardwareDevice> HardwareDevices => Set<HardwareDevice>();
    public DbSet<HardwareDeviceAssignment> HardwareDeviceAssignments => Set<HardwareDeviceAssignment>();
    public DbSet<HardwareTestLog> HardwareTestLogs => Set<HardwareTestLog>();
    public DbSet<TillSession> TillSessions => Set<TillSession>();

    // Catalog and Product
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<BusinessTypeOptionTemplate> BusinessTypeOptionTemplates => Set<BusinessTypeOptionTemplate>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ChoiceGroup> ChoiceGroups => Set<ChoiceGroup>();
    public DbSet<ChoiceOption> ChoiceOptions => Set<ChoiceOption>();
    public DbSet<ChoiceOptionInventoryImpact> ChoiceOptionInventoryImpacts => Set<ChoiceOptionInventoryImpact>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<ComboComponent> ComboComponents => Set<ComboComponent>();
    public DbSet<ComboDefinition> ComboDefinitions => Set<ComboDefinition>();
    public DbSet<ComboGroup> ComboGroups => Set<ComboGroup>();
    public DbSet<ComboGroupItem> ComboGroupItems => Set<ComboGroupItem>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAttributeDefinition> ProductAttributeDefinitions => Set<ProductAttributeDefinition>();
    public DbSet<ProductAttributeOption> ProductAttributeOptions => Set<ProductAttributeOption>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductAttributeValueOption> ProductAttributeValueOptions => Set<ProductAttributeValueOption>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductChannelVisibility> ProductChannelVisibilities => Set<ProductChannelVisibility>();
    public DbSet<ProductChoiceGroup> ProductChoiceGroups => Set<ProductChoiceGroup>();
    public DbSet<ProductChoiceOption> ProductChoiceOptions => Set<ProductChoiceOption>();
    public DbSet<ProductCollection> ProductCollections => Set<ProductCollection>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<ProductOptionTemplate> ProductOptionTemplates => Set<ProductOptionTemplate>();
    public DbSet<ProductOptionTemplateValue> ProductOptionTemplateValues => Set<ProductOptionTemplateValue>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantOptionValue> ProductVariantOptionValues => Set<ProductVariantOptionValue>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductRatingSummary> ProductRatingSummaries => Set<ProductRatingSummary>();

    public DbSet<ReturnPolicy> ReturnPolicies => Set<ReturnPolicy>();
    public DbSet<ReturnPolicyTemplate> ReturnPolicyTemplates => Set<ReturnPolicyTemplate>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();



    // Pricing and Tax
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListChannel> PriceListChannels => Set<PriceListChannel>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<PriceListOutlet> PriceListOutlets => Set<PriceListOutlet>();
    public DbSet<ProductTaxAssignment> ProductTaxAssignments => Set<ProductTaxAssignment>();
    public DbSet<TaxClass> TaxClasses => Set<TaxClass>();
    public DbSet<TaxClassRate> TaxClassRates => Set<TaxClassRate>();
    public DbSet<TaxJurisdiction> TaxJurisdictions => Set<TaxJurisdiction>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    // Discount
    public DbSet<DiscountPolicy> DiscountPolicies => Set<DiscountPolicy>();
    public DbSet<DiscountPolicyChannel> DiscountPolicyChannels => Set<DiscountPolicyChannel>();
    public DbSet<DiscountPolicyCondition> DiscountPolicyConditions => Set<DiscountPolicyCondition>();
    public DbSet<DiscountPolicyOutlet> DiscountPolicyOutlets => Set<DiscountPolicyOutlet>();
    public DbSet<DiscountPolicyTarget> DiscountPolicyTargets => Set<DiscountPolicyTarget>();
    public DbSet<DiscountType> DiscountTypes => Set<DiscountType>();
    public DbSet<ExpiryDiscountApplication> ExpiryDiscountApplications => Set<ExpiryDiscountApplication>();
    public DbSet<ExpiryDiscountRule> ExpiryDiscountRules => Set<ExpiryDiscountRule>();
    public DbSet<ExpiryDiscountRuleTier> ExpiryDiscountRuleTiers => Set<ExpiryDiscountRuleTier>();

    // Inventory
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryChannelAllocation> InventoryChannelAllocations => Set<InventoryChannelAllocation>();
    public DbSet<InventoryCostLayer> InventoryCostLayers => Set<InventoryCostLayer>();
    public DbSet<InventoryLocation> InventoryLocations => Set<InventoryLocation>();
    public DbSet<InventoryReorderRule> InventoryReorderRules => Set<InventoryReorderRule>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<InventoryReservationAllocation> InventoryReservationAllocations => Set<InventoryReservationAllocation>();
    public DbSet<InventoryReservationLine> InventoryReservationLines => Set<InventoryReservationLine>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<ProductInventorySetting> ProductInventorySettings => Set<ProductInventorySetting>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentLine> StockAdjustmentLines => Set<StockAdjustmentLine>();
    public DbSet<StockAdjustmentReason> StockAdjustmentReasons => Set<StockAdjustmentReason>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockMovementCostAllocation> StockMovementCostAllocations => Set<StockMovementCostAllocation>();
    public DbSet<StockMovementReference> StockMovementReferences => Set<StockMovementReference>();
    public DbSet<StockMovementSerial> StockMovementSerials => Set<StockMovementSerial>();
    public DbSet<StocktakeLine> StocktakeLines => Set<StocktakeLine>();
    public DbSet<StocktakeLineSerial> StocktakeLineSerials => Set<StocktakeLineSerial>();
    public DbSet<StocktakeSession> StocktakeSessions => Set<StocktakeSession>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
    public DbSet<StockTransferStatusHistory> StockTransferStatusHistory => Set<StockTransferStatusHistory>();

    // Customer
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAuthAccount> CustomerAuthAccounts => Set<CustomerAuthAccount>();
    public DbSet<CustomerAuthSession> CustomerAuthSessions => Set<CustomerAuthSession>();
    public DbSet<CustomerConsent> CustomerConsents => Set<CustomerConsent>();
    public DbSet<CustomerPasswordResetToken> CustomerPasswordResetTokens => Set<CustomerPasswordResetToken>();
    public DbSet<CustomerRefreshToken> CustomerRefreshTokens => Set<CustomerRefreshToken>();
    public DbSet<CustomerVerificationOtp> CustomerVerificationOtps => Set<CustomerVerificationOtp>();
    public DbSet<CustomerWishlist> CustomerWishlists => Set<CustomerWishlist>();
    public DbSet<CustomerWishlistItem> CustomerWishlistItems => Set<CustomerWishlistItem>();


    // Orders and Sales
    public DbSet<DocumentNumberSequence> DocumentNumberSequences => Set<DocumentNumberSequence>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderDiscount> SalesOrderDiscounts => Set<SalesOrderDiscount>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<SalesOrderLineComponent> SalesOrderLineComponents => Set<SalesOrderLineComponent>();
    public DbSet<SalesOrderLineOption> SalesOrderLineOptions => Set<SalesOrderLineOption>();
    public DbSet<SalesOrderLineStatusHistory> SalesOrderLineStatusHistory => Set<SalesOrderLineStatusHistory>();
    public DbSet<SalesOrderStatusHistory> SalesOrderStatusHistory => Set<SalesOrderStatusHistory>();
    public DbSet<SalesOrderTax> SalesOrderTaxes => Set<SalesOrderTax>();
    public DbSet<SalesOrderCharge> SalesOrderCharges => Set<SalesOrderCharge>();

    // POS Operations
    public DbSet<PosOrderHold> PosOrderHolds => Set<PosOrderHold>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptPrintLog> ReceiptPrintLogs => Set<ReceiptPrintLog>();
    public DbSet<ReceiptTemplate> ReceiptTemplates => Set<ReceiptTemplate>();
    public DbSet<ReceiptTemplateAssignment> ReceiptTemplateAssignments => Set<ReceiptTemplateAssignment>();
    public DbSet<ReceiptTemplateVersion> ReceiptTemplateVersions => Set<ReceiptTemplateVersion>();
    public DbSet<TillCashMovement> TillCashMovements => Set<TillCashMovement>();
    public DbSet<TillSessionEvent> TillSessionEvents => Set<TillSessionEvent>();
    public DbSet<TillSessionPaymentSummary> TillSessionPaymentSummaries => Set<TillSessionPaymentSummary>();
    public DbSet<TillSessionSummary> TillSessionSummaries => Set<TillSessionSummary>();

    // Cart and Checkout
    public DbSet<CheckoutEvent> CheckoutEvents => Set<CheckoutEvent>();
    public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>();
    public DbSet<CheckoutSessionAddress> CheckoutSessionAddresses => Set<CheckoutSessionAddress>();
    public DbSet<CheckoutSessionLine> CheckoutSessionLines => Set<CheckoutSessionLine>();    public DbSet<CheckoutSessionLineOption> CheckoutSessionLineOptions => Set<CheckoutSessionLineOption>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();    public DbSet<ShoppingCartItemOption> ShoppingCartItemOptions => Set<ShoppingCartItemOption>();

    // Storefront
    public DbSet<StorefrontBanner> StorefrontBanners => Set<StorefrontBanner>();

    // Fulfilment and Pickup
    public DbSet<FulfillmentMethod> FulfillmentMethods => Set<FulfillmentMethod>();
    public DbSet<FulfillmentMethodOutlet> FulfillmentMethodOutlets => Set<FulfillmentMethodOutlet>();
    public DbSet<FulfillmentOrder> FulfillmentOrders => Set<FulfillmentOrder>();
    public DbSet<FulfillmentOrderEvent> FulfillmentOrderEvents => Set<FulfillmentOrderEvent>();
    public DbSet<FulfillmentOrderLine> FulfillmentOrderLines => Set<FulfillmentOrderLine>();
    public DbSet<PickupOrder> PickupOrders => Set<PickupOrder>();
    public DbSet<PickupOrderEvent> PickupOrderEvents => Set<PickupOrderEvent>();
    public DbSet<PickupSlot> PickupSlots => Set<PickupSlot>();
    public DbSet<PickupSlotReservation> PickupSlotReservations => Set<PickupSlotReservation>();

    // Payment
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<SalesPayment> SalesPayments => Set<SalesPayment>();
    public DbSet<SalesPaymentEvent> SalesPaymentEvents => Set<SalesPaymentEvent>();
    public DbSet<SalesPaymentTransaction> SalesPaymentTransactions => Set<SalesPaymentTransaction>();

    // Refund
    public DbSet<SalesRefund> SalesRefunds => Set<SalesRefund>();
    public DbSet<SalesRefundLine> SalesRefundLines => Set<SalesRefundLine>();
    public DbSet<SalesRefundPaymentAllocation> SalesRefundPaymentAllocations => Set<SalesRefundPaymentAllocation>();

    // Return, Inspection and Exchange
    public DbSet<ReturnInspection> ReturnInspections => Set<ReturnInspection>();
    public DbSet<ReturnReason> ReturnReasons => Set<ReturnReason>();
    public DbSet<SalesExchange> SalesExchanges => Set<SalesExchange>();
    public DbSet<SalesExchangeEvent> SalesExchangeEvents => Set<SalesExchangeEvent>();
    public DbSet<SalesExchangeLine> SalesExchangeLines => Set<SalesExchangeLine>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnEvent> SalesReturnEvents => Set<SalesReturnEvent>();
    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();

    // Notification
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();
    public DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts => Set<NotificationDeliveryAttempt>();
    public DbSet<NotificationEvent> NotificationEvents => Set<NotificationEvent>();
    public DbSet<NotificationEventType> NotificationEventTypes => Set<NotificationEventType>();
    public DbSet<NotificationInboxItem> NotificationInboxItems => Set<NotificationInboxItem>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationReadReceipt> NotificationReadReceipts => Set<NotificationReadReceipt>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationTemplateVersion> NotificationTemplateVersions => Set<NotificationTemplateVersion>();

    // Integration
    public DbSet<IntegrationProvider> IntegrationProviders => Set<IntegrationProvider>();
    public DbSet<PlatformIntegration> PlatformIntegrations => Set<PlatformIntegration>();
    public DbSet<PlatformIntegrationCredential> PlatformIntegrationCredentials => Set<PlatformIntegrationCredential>();
    public DbSet<PlatformIntegrationRequestLog> PlatformIntegrationRequestLogs => Set<PlatformIntegrationRequestLog>();
    public DbSet<PlatformIntegrationWebhookEvent> PlatformIntegrationWebhookEvents => Set<PlatformIntegrationWebhookEvent>();

    // Offline Sync
    public DbSet<DeviceSyncState> DeviceSyncStates => Set<DeviceSyncState>();
    public DbSet<OfflineClient> OfflineClients => Set<OfflineClient>();
    public DbSet<OfflineIdMapping> OfflineIdMappings => Set<OfflineIdMapping>();
    public DbSet<OfflineNumberBlock> OfflineNumberBlocks => Set<OfflineNumberBlock>();
    public DbSet<SyncBatch> SyncBatches => Set<SyncBatch>();
    public DbSet<SyncConflict> SyncConflicts => Set<SyncConflict>();
    public DbSet<SyncItem> SyncItems => Set<SyncItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EPosDbContext).Assembly);
    }
}




