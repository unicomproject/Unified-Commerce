#nullable enable
namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

public enum CashierPosPermissionDefinitionKind
{
    Existing,
    New,
    Split,
    DocumentedResolved,
    PreAuth,
}
