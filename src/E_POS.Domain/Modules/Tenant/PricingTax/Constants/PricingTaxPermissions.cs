namespace E_POS.Domain.Modules.Tenant.PricingTax.Constants;

public static class PricingTaxPermissions
{
    public static class TaxClasses
    {
        public const string View = "tax.classes.view";
        public const string Create = "tax.classes.create";
        public const string Update = "tax.classes.update";
        public const string Delete = "tax.classes.delete";
    }

    public static class TaxRates
    {
        public const string View = "tax.rates.view";
        public const string Create = "tax.rates.create";
        public const string Update = "tax.rates.update";
        public const string Delete = "tax.rates.delete";
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

