using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;

public sealed class TenantAdminHardwareAuditLogger : ITenantAdminHardwareAuditLogger
{
    private readonly ILogger<TenantAdminHardwareAuditLogger> _logger;

    public TenantAdminHardwareAuditLogger(ILogger<TenantAdminHardwareAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogHardwareCreated(
        Guid tenantId,
        Guid? actorUserId,
        Guid hardwareDeviceId,
        string deviceCode,
        string hardwareType)
    {
        _logger.LogInformation(
            "HARDWARE_CREATED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} HardwareDeviceId={HardwareDeviceId}",
            tenantId,
            actorUserId,
            hardwareDeviceId);
    }

    public void LogHardwareAssigned(
        Guid tenantId,
        Guid? actorUserId,
        Guid assignmentId,
        Guid hardwareDeviceId,
        Guid? tillId,
        Guid? posDeviceId)
    {
        _logger.LogInformation(
            "HARDWARE_ASSIGNED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} AssignmentId={AssignmentId} HardwareDeviceId={HardwareDeviceId} TillId={TillId} PosDeviceId={PosDeviceId}",
            tenantId,
            actorUserId,
            assignmentId,
            hardwareDeviceId,
            tillId,
            posDeviceId);
    }

    public void LogHardwareReleased(
        Guid tenantId,
        Guid? actorUserId,
        Guid assignmentId,
        Guid hardwareDeviceId,
        string? reason)
    {
        _logger.LogInformation(
            "HARDWARE_RELEASED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} AssignmentId={AssignmentId} HardwareDeviceId={HardwareDeviceId}",
            tenantId,
            actorUserId,
            assignmentId,
            hardwareDeviceId);
    }

    public void LogHardwareHeartbeat(
        Guid tenantId,
        Guid posDeviceId,
        Guid hardwareDeviceId,
        string? warningCode)
    {
        _logger.LogInformation(
            "HARDWARE_HEARTBEAT TenantId={TenantId} PosDeviceId={PosDeviceId} HardwareDeviceId={HardwareDeviceId}",
            tenantId,
            posDeviceId,
            hardwareDeviceId);
    }

    public void LogHardwareTestFailed(
        Guid tenantId,
        Guid hardwareDeviceId,
        string testType,
        string? message)
    {
        _logger.LogInformation(
            "HARDWARE_TEST_FAILED TenantId={TenantId} HardwareDeviceId={HardwareDeviceId}",
            tenantId,
            hardwareDeviceId);
    }
}
