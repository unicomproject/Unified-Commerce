#nullable enable
namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

public sealed record CashierPosPermissionDefinition(
    string Code,
    string? ParentCode,
    string Module,
    CashierPosPermissionSemanticType SemanticType,
    bool IsSensitive,
    CashierPosPermissionDefinitionKind Kind,
    string Reason,
    bool IsRoleAssignable);
