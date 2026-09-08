namespace E_POS.Domain.Modules.Tenant.AccessControl.Constants;

public static class CustomerPermissions
{
    public const string View = "pos.customers.management.view";
    public const string Create = "pos.customers.management.create";
    public const string Update = "pos.customers.management.update";

    /// <summary>Chunk 2 fine-grained customer actions. Chunk 6 enforces attach/deactivate where endpoints exist.</summary>
    public const string AttachSale = "pos.customers.management.attach_sale";
    public const string Deactivate = "pos.customers.management.deactivate";
}
