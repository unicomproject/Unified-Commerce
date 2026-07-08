namespace E_POS.Infrastructure.Persistence.Seed;

public sealed record TenantPermissionSeedDefinition(
    Guid Id,
    string PermissionCode,
    Guid ModuleId,
    Guid FeatureId,
    string ActionType,
    string Description);
