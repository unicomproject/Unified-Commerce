namespace E_POS.Application.Common.Contracts;

public interface ITenantContext
{
    Guid? TenantId { get; }

    bool HasTenant => TenantId.HasValue;
}