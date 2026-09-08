namespace E_POS.Domain.Modules.Tenant.PricingTax.Constants;

public static class PricingTaxPermissions
{
    public static class TaxClasses
    {
        /// <summary>TARGET canonical permission (Tax Setup view).</summary>
        public const string View = "pricing.tax_classes.view";
        public const string Create = "pricing.tax_classes.create";
        public const string Update = "pricing.tax_classes.update";
        public const string StatusManage = "pricing.tax_classes.status.manage";
        public const string ProductsView = "pricing.tax_classes.products.view";

        /// <summary>Legacy runtime codes (compatibility / seeded historically).</summary>
        public const string LegacyView = "tax.classes.view";
        public const string LegacyCreate = "tax.classes.create";
        public const string LegacyUpdate = "tax.classes.update";
        public const string LegacyDelete = "tax.classes.delete";
        public const string LegacyManage = "tax.classes.manage";
    }

    public static class TaxRates
    {
        public const string View = "pricing.tax_rates.view";
        public const string ScheduleManage = "pricing.tax_rates.schedule.manage";

        public const string LegacyView = "tax.rates.view";
        public const string LegacyCreate = "tax.rates.create";
        public const string LegacyUpdate = "tax.rates.update";
        public const string LegacyDelete = "tax.rates.delete";
        public const string LegacyManage = "tax.rates.manage";
    }

    public static class ProductTaxAssignments
    {
        public const string View = "pricing.product_tax_assignments.view";
        public const string Create = "pricing.product_tax_assignments.create";
        public const string Update = "pricing.product_tax_assignments.update";
        public const string Delete = "pricing.product_tax_assignments.delete";
        public const string Manage = "pricing.product_tax_assignments.manage";
    }
}
