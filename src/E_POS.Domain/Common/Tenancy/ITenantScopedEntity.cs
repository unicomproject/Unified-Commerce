namespace E_POS.Domain.Common.Tenancy;

public interface ITenantScopedEntity
{
    Guid TenantId { get; }
}