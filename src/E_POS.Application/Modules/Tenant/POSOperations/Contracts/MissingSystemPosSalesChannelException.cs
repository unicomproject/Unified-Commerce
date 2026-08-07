namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public sealed class MissingSystemPosSalesChannelException : Exception
{
    public MissingSystemPosSalesChannelException(Guid tenantId)
        : base("Required system POS sales channel configuration is unavailable.")
    {
        TenantId = tenantId;
    }

    public Guid TenantId { get; }

    public string Operation => "ParkSale";

    public string ConfigurationType => "PlatformSalesChannel:POS";
}
