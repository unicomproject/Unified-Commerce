namespace E_POS.Domain.Modules.ECommerce.Storefront.Constants;

public static class TenantAdminOnlineStorePermissions
{
    public const string View = "tenant.online_store.view";
    public const string Manage = "tenant.online_store.manage";
    public const string Publish = "tenant.online_store.publish";
    public const string DomainsManage = "tenant.online_store.domains.manage";
    public const string BrandingManage = "tenant.online_store.branding.manage";
    public const string SupportManage = "tenant.online_store.support.manage";
    public const string FulfillmentManage = "tenant.online_store.fulfillment.manage";
    public const string CatalogManage = "tenant.online_store.catalog.manage";
    public const string PoliciesManage = "tenant.online_store.policies.manage";

    public static readonly IReadOnlyList<string> All =
    [
        View,
        Manage,
        Publish,
        DomainsManage,
        BrandingManage,
        SupportManage,
        FulfillmentManage,
        CatalogManage,
        PoliciesManage
    ];
}
