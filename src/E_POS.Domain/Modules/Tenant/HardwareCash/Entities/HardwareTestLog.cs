using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class HardwareTestLog : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid? HardwareDeviceId { get; protected set; }
    public Guid? InitiatedFromPosDeviceId { get; protected set; }
    public Guid? TillId { get; protected set; }
    public Guid? TillSessionId { get; protected set; }
    public Guid? TestedByTenantUserId { get; protected set; }
    public Guid RequestId { get; protected set; }
    public string RequestPayloadHash { get; protected set; } = string.Empty;
    public int ConfigurationVersion { get; protected set; }
    public string HardwareType { get; protected set; } = string.Empty;
    public string TestType { get; protected set; } = string.Empty;
    public string TestStatus { get; protected set; } = string.Empty;
    public string? ResultCategory { get; protected set; }
    public string? ResultMessage { get; protected set; }
    public string? ResultPayloadJson { get; protected set; }
    public DateTimeOffset TestedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public bool? PhysicalConfirmation { get; protected set; }

    public static HardwareTestLog Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid? hardwareDeviceId,
        Guid? initiatedFromPosDeviceId,
        Guid? tillId,
        Guid? tillSessionId,
        Guid? testedByTenantUserId,
        Guid requestId,
        string requestPayloadHash,
        int configurationVersion,
        string hardwareType,
        string testType,
        string testStatus,
        string? resultCategory,
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
            TillId = tillId,
            TillSessionId = tillSessionId,
            TestedByTenantUserId = testedByTenantUserId,
            RequestId = requestId,
            RequestPayloadHash = requestPayloadHash,
            ConfigurationVersion = configurationVersion,
            HardwareType = hardwareType.Trim().ToUpperInvariant(),
            TestType = testType.Trim().ToUpperInvariant(),
            TestStatus = testStatus.Trim().ToUpperInvariant(),
            ResultCategory = resultCategory?.Trim().ToUpperInvariant(),
            ResultMessage = resultMessage?.Trim(),
            ResultPayloadJson = resultPayloadJson,
            TestedAt = testedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Complete(
        string status,
        string resultCategory,
        string? safeMessage,
        string? safePayloadJson,
        bool? physicalConfirmation,
        DateTimeOffset now)
    {
        if (TestStatus is "PASSED" or "FAILED" or "CANCELLED" or "EXPIRED" or "BLOCKED")
            return;

        TestStatus = status.Trim().ToUpperInvariant();
        ResultCategory = resultCategory.Trim().ToUpperInvariant();
        ResultMessage = safeMessage?.Trim();
        ResultPayloadJson = safePayloadJson;
        PhysicalConfirmation = physicalConfirmation;
        CompletedAt = now;
    }
}

