using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class HardwareTestLog : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid HardwareDeviceId { get; protected set; }
    public Guid? InitiatedFromPosDeviceId { get; protected set; }
    public Guid? TestedByTenantUserId { get; protected set; }
    public string TestType { get; protected set; } = string.Empty;
    public string TestStatus { get; protected set; } = string.Empty;
    public string? ResultMessage { get; protected set; }
    public string? ResultPayloadJson { get; protected set; }
    public DateTimeOffset TestedAt { get; protected set; }

    public static HardwareTestLog Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid hardwareDeviceId,
        Guid? initiatedFromPosDeviceId,
        Guid? testedByTenantUserId,
        string testType,
        string testStatus,
        string? resultMessage,
        string? resultPayloadJson,
        DateTimeOffset testedAt,
        DateTimeOffset now)
    {
        return new HardwareTestLog
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            HardwareDeviceId = hardwareDeviceId,
            InitiatedFromPosDeviceId = initiatedFromPosDeviceId,
            TestedByTenantUserId = testedByTenantUserId,
            TestType = testType.Trim().ToUpperInvariant(),
            TestStatus = testStatus.Trim().ToUpperInvariant(),
            ResultMessage = resultMessage?.Trim(),
            ResultPayloadJson = resultPayloadJson,
            TestedAt = testedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

