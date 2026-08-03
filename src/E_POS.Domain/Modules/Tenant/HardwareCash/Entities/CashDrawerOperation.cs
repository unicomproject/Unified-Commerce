using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class CashDrawerOperation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid? HardwareDeviceId { get; protected set; }
    public Guid PosDeviceId { get; protected set; }
    public Guid TillId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
    public Guid ProcessedByUserId { get; protected set; }
    public Guid? ApproverId { get; protected set; }
    public Guid RequestId { get; protected set; }
    public string DrawerPurpose { get; protected set; } = string.Empty;
    public string? Reason { get; protected set; }
    public string? BusinessReferenceType { get; protected set; }
    public Guid? BusinessReferenceId { get; protected set; }
    public Guid? ConfigurationId { get; protected set; }
    public int ConfigurationVersion { get; protected set; }
    public string DrawerPort { get; protected set; } = string.Empty;
    public int PulseOnTime { get; protected set; }
    public int PulseOffTime { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string? ResultCategory { get; protected set; }
    public string? FailureCategory { get; protected set; }
    public bool AgentAccepted { get; protected set; }
    public bool? PhysicalConfirmation { get; protected set; }
    public DateTimeOffset InitiatedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public string PayloadHash { get; protected set; } = string.Empty;

    public static CashDrawerOperation Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid? hardwareDeviceId,
        Guid posDeviceId,
        Guid tillId,
        Guid tillSessionId,
        Guid processedByUserId,
        Guid? approverId,
        Guid requestId,
        string drawerPurpose,
        string? reason,
        string? businessReferenceType,
        Guid? businessReferenceId,
        Guid? configurationId,
        int configurationVersion,
        string drawerPort,
        int pulseOnTime,
        int pulseOffTime,
        string status,
        string? resultCategory,
        string? failureCategory,
        bool agentAccepted,
        bool? physicalConfirmation,
        DateTimeOffset initiatedAt,
        string payloadHash,
        DateTimeOffset now)
    {
        return new CashDrawerOperation
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            HardwareDeviceId = hardwareDeviceId,
            PosDeviceId = posDeviceId,
            TillId = tillId,
            TillSessionId = tillSessionId,
            ProcessedByUserId = processedByUserId,
            ApproverId = approverId,
            RequestId = requestId,
            DrawerPurpose = drawerPurpose.Trim(),
            Reason = reason?.Trim(),
            BusinessReferenceType = businessReferenceType?.Trim(),
            BusinessReferenceId = businessReferenceId,
            ConfigurationId = configurationId,
            ConfigurationVersion = configurationVersion,
            DrawerPort = drawerPort.Trim(),
            PulseOnTime = pulseOnTime,
            PulseOffTime = pulseOffTime,
            Status = status.Trim().ToUpperInvariant(),
            ResultCategory = resultCategory?.Trim().ToUpperInvariant(),
            FailureCategory = failureCategory?.Trim().ToUpperInvariant(),
            AgentAccepted = agentAccepted,
            PhysicalConfirmation = physicalConfirmation,
            InitiatedAt = initiatedAt,
            PayloadHash = payloadHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void FinalizeOperation(
        string status,
        string? resultCategory,
        string? failureCategory,
        bool agentAccepted,
        bool? physicalConfirmation,
        DateTimeOffset now)
    {
        Status = status.Trim().ToUpperInvariant();
        ResultCategory = resultCategory?.Trim().ToUpperInvariant();
        FailureCategory = failureCategory?.Trim().ToUpperInvariant();
        AgentAccepted = agentAccepted;
        PhysicalConfirmation = physicalConfirmation;
        CompletedAt = now;
        UpdatedAt = now;
    }
}
