namespace E_POS.Application.Modules.Tenant.HardwareCash.Dtos;

public sealed record RegisterDrawerOperationRequest(
    Guid RequestId,
    Guid PosDeviceId,
    Guid? TillId,
    string DrawerPurpose,
    string? Reason = null,
    string? BusinessReferenceType = null,
    Guid? BusinessReferenceId = null);

public sealed record FinalizeDrawerOperationRequest(
    string Status,
    string? ResultCategory = null,
    string? FailureCategory = null,
    bool AgentAccepted = false,
    bool? PhysicalConfirmation = null);

public sealed record ManualOpenDrawerRequest(
    Guid RequestId,
    Guid PosDeviceId,
    string Reason,
    string? ManagerEmail = null,
    string? ManagerPassword = null);

public sealed record CashDrawerOperationDto(
    Guid OperationId,
    Guid TenantId,
    Guid OutletId,
    Guid? HardwareDeviceId,
    Guid PosDeviceId,
    Guid TillId,
    Guid TillSessionId,
    Guid ProcessedByUserId,
    Guid? ApproverId,
    Guid RequestId,
    string DrawerPurpose,
    string? Reason,
    string? BusinessReferenceType,
    Guid? BusinessReferenceId,
    Guid? ConfigurationId,
    int ConfigurationVersion,
    string DrawerPort,
    int PulseOnTime,
    int PulseOffTime,
    string Status,
    string? ResultCategory,
    string? FailureCategory,
    bool AgentAccepted,
    bool? PhysicalConfirmation,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? CompletedAt);
