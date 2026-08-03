using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

public interface ITenantAdminHardwareService
{
    Task<ApplicationResult<TenantAdminHardwareDeviceListResponse>> ListAsync(
        TenantRequestContext context,
        Guid? outletId,
        string? hardwareType,
        string? lifecycleStatus,
        string? assignmentStatus,
        bool? availableOnly,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminHardwareDeviceDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminHardwareDeviceDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminHardwareDeviceCreateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminHardwareAssignmentResponse>> AssignToTillAsync(
        TenantRequestContext context,
        Guid tillId,
        TenantAdminHardwareAssignmentRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminHardwareAssignmentResponse>> AssignToPosDeviceAsync(
        TenantRequestContext context,
        Guid posDeviceId,
        TenantAdminHardwareAssignmentRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminHardwareAssignmentResponse>> ReleaseAssignmentAsync(
        TenantRequestContext context,
        Guid assignmentId,
        TenantAdminHardwareAssignmentReleaseRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosHardwareHeartbeatResponse>> RecordHardwareHeartbeatAsync(
        TenantRequestContext context,
        Guid posDeviceId,
        PosHardwareHeartbeatRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosHardwareTestResultResponse>> ReportHardwareTestAsync(
        TenantRequestContext context,
        PosHardwareTestResultRequest request,
        CancellationToken cancellationToken);
}

public interface ITenantAdminHardwareRepository
{
    Task<bool> OutletBelongsToTenantAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken);

    Task<bool> DeviceCodeExistsAsync(
        Guid tenantId,
        string hardwareDeviceCode,
        Guid? excludeDeviceId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<HardwareDeviceListRow> Items, int TotalCount)> ListAsync(
        Guid tenantId,
        Guid? outletId,
        string? hardwareType,
        string? lifecycleStatus,
        string? assignmentStatus,
        bool? availableOnly,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<HardwareDeviceDetailRow?> GetDetailAsync(
        Guid tenantId,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken);

    Task AddDeviceAsync(HardwareDevice device, CancellationToken cancellationToken);

    Task<HardwareDevice?> GetEditableDeviceAsync(
        Guid tenantId,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken);

    Task<Till?> GetTillAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken);

    Task<PosDevice?> GetPosDeviceAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken);

    Task<HardwareDeviceAssignment?> GetActiveAssignmentForDeviceAsync(
        Guid tenantId,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken);

    Task<HardwareDeviceAssignment?> GetAssignmentAsync(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken);

    Task AddAssignmentAsync(HardwareDeviceAssignment assignment, CancellationToken cancellationToken);

    Task AddTestLogAsync(HardwareTestLog testLog, CancellationToken cancellationToken);

    Task<bool> IsHardwareLinkedToPosDeviceAsync(
        Guid tenantId,
        Guid posDeviceId,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record HardwareDeviceListRow(
    HardwareDevice Device,
    string OutletName,
    HardwareDeviceAssignment? ActiveAssignment);

public sealed record HardwareDeviceDetailRow(
    HardwareDevice Device,
    string OutletName,
    HardwareDeviceAssignment? ActiveAssignment);

public interface ITenantAdminHardwareAuditLogger
{
    void LogHardwareCreated(Guid tenantId, Guid? actorUserId, Guid hardwareDeviceId, string deviceCode, string hardwareType);
    void LogHardwareAssigned(Guid tenantId, Guid? actorUserId, Guid assignmentId, Guid hardwareDeviceId, Guid? tillId, Guid? posDeviceId);
    void LogHardwareReleased(Guid tenantId, Guid? actorUserId, Guid assignmentId, Guid hardwareDeviceId, string? reason);
    void LogHardwareHeartbeat(Guid tenantId, Guid posDeviceId, Guid hardwareDeviceId, string? warningCode);
    void LogHardwareTestFailed(Guid tenantId, Guid hardwareDeviceId, string testType, string? message);
}
