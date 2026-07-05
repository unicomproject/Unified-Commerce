namespace E_POS.Domain.Modules.PricingTax.Constants;

public static class PricingTaxPermissions
{
    public static class TaxClasses
    {
        public const string View = "pricing.tax_classes.view";
        public const string Create = "pricing.tax_classes.create";
        public const string Update = "pricing.tax_classes.update";
        public const string Delete = "pricing.tax_classes.delete";
    }

    public static class TaxRates
    {
        public const string View = "pricing.tax_rates.view";
        public const string Create = "pricing.tax_rates.create";
        public const string Update = "pricing.tax_rates.update";
        public const string Delete = "pricing.tax_rates.delete";
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
