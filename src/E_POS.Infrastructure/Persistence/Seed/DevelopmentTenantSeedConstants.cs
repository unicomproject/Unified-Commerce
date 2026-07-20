namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentTenantSeedConstants
{
    public static readonly Guid DevelopmentTenantId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    public static readonly Guid CashierRoleId = Guid.Parse("88888888-0003-4000-8000-000000000001");
    public static readonly Guid CashierUserId = Guid.Parse("99999999-0003-4000-8000-000000000001");
    public static readonly Guid CashierTwoUserId = Guid.Parse("99999999-0006-4000-8000-000000000001");
    public const string CashierRoleCode = "CASHIER";
    public const string CashierEmail = "CASHIER001@GMAIL.COM";
    public const string CashierTwoEmail = "CASHIER002@GMAIL.COM";
}

public static class DevelopmentPosPermissionCatalogSeedConstants
{
    public static readonly Guid CorePosModuleId = Guid.Parse("71000000-0000-0000-0000-000000000010");

    public static readonly Guid PosHomeFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000010");
    public static readonly Guid PosSalesFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000011");
    public static readonly Guid PosProductsFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000012");
    public static readonly Guid PosCustomersFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000013");
    public static readonly Guid PosPaymentsFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000014");
    public static readonly Guid PosReceiptsFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000015");
    public static readonly Guid PosOrdersFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000016");
    public static readonly Guid PosReturnsFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000017");
    public static readonly Guid PosCashDrawerFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000018");
    public static readonly Guid PosTillFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000019");
    public static readonly Guid PosNotificationsFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000020");
    public static readonly Guid TenantTillOpsFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000021");
    public static readonly Guid PosExchangesFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000022");
}
