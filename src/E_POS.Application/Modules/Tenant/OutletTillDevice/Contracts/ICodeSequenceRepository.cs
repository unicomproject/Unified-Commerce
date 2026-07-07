namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ICodeSequenceRepository
{
    Task<string> GetNextCodeAsync(Guid tenantId, string sequenceKey, string prefix, int paddingLength, DateTimeOffset now, CancellationToken cancellationToken);
}
