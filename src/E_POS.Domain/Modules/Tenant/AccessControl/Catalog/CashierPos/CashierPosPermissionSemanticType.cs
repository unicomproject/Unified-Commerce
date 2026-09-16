#nullable enable
namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

public enum CashierPosPermissionSemanticType
{
    Screen,
    Section,
    Action,
    Field,
    SensitiveField,
    Navigation,
    Container,
    Message,
    Input,
    Control,
    Status,
    PreAuthConfiguration,
}
