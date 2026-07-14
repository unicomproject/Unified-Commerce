namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentProductCatalogSeedConstants
{
    public static readonly Guid DevelopmentTenantId = DevelopmentTenantSeedConstants.DevelopmentTenantId;
    public static readonly Guid DevelopmentTenantAdminUserId = Guid.Parse("99999999-0001-4000-8000-000000000001");

    public static readonly Guid GeneralDepartmentId = Guid.Parse("cccccccc-0001-4000-8000-000000000001");

    public static readonly Guid GroceriesCategoryId = Guid.Parse("cccccccc-0002-4000-8000-000000000001");
    public static readonly Guid ElectronicsCategoryId = Guid.Parse("cccccccc-0003-4000-8000-000000000001");

    public static readonly Guid BeveragesSubCategoryId = Guid.Parse("cccccccc-0011-4000-8000-000000000001");
    public static readonly Guid SnacksSubCategoryId = Guid.Parse("cccccccc-0012-4000-8000-000000000001");
    public static readonly Guid MobilePhonesSubCategoryId = Guid.Parse("cccccccc-0021-4000-8000-000000000001");

    public static readonly Guid UnileverBrandId = Guid.Parse("cccccccc-0101-4000-8000-000000000001");
    public static readonly Guid SamsungBrandId = Guid.Parse("cccccccc-0102-4000-8000-000000000001");
}
