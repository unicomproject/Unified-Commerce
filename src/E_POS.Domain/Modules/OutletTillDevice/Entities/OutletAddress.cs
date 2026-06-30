using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class OutletAddress : AuditableEntity
{
    public Guid? OutletId { get; protected set; }
    public string AddressType { get; protected set; } = string.Empty;
}
