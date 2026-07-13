using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;

public sealed class PosTillSessionRepository : IPosTillSessionRepository
{
    private const string TillSessionNumberPrefix = "TS-";
    private const int TillSessionNumberPadding = 4;

    private readonly EPosDbContext _dbContext;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly ILogger<PosTillSessionRepository> _logger;

    public PosTillSessionRepository(
        EPosDbContext dbContext,
        ICodeSequenceRepository codeSequenceRepository,
        ILogger<PosTillSessionRepository> logger)
    {
        _dbContext = dbContext;
        _codeSequenceRepository = codeSequenceRepository;
        _logger = logger;
    }

    public async Task<CurrentTillSessionResolveResult> ResolveCurrentSessionAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var deviceContext = await ResolveTrustedDeviceAssignmentAsync(tenantId, deviceId, cancellationToken);
        if (!deviceContext.IsSuccess || deviceContext.Assignment is null)
        {
            return ResolveFailure(deviceContext.ErrorCode!);
        }

        var session = await FindOpenSessionAsync(tenantId, deviceContext.Assignment.TillId, cancellationToken);
        if (session is null)
        {
            _logger.LogDebug(
                "Current till session unresolved: no open session for till {TillId}.",
                deviceContext.Assignment.TillId);
            return ResolveFailure("till_session.not_found");
        }

        _logger.LogDebug(
            "Current till session resolved {SessionId} for device {DeviceId}, till {TillId}.",
            session.Id,
            deviceId,
            deviceContext.Assignment.TillId);

        return ResolveSuccess(MapSnapshot(session));
    }

    public async Task<OpenTillRepositoryResult> OpenTillAsync(
        Guid tenantId,
        Guid tenantUserId,
        OpenTillCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var deviceContext = await ResolveTrustedDeviceAssignmentAsync(tenantId, command.DeviceId, cancellationToken);
        if (!deviceContext.IsSuccess || deviceContext.Assignment is null)
        {
            return OpenFailure(deviceContext.ErrorCode!);
        }

        if (deviceContext.Assignment.TillId != command.TillId)
        {
            _logger.LogDebug(
                "Till open rejected: device {DeviceId} assigned to till {AssignedTillId}, requested {RequestedTillId}.",
                command.DeviceId,
                deviceContext.Assignment.TillId,
                command.TillId);
            return OpenFailure("till_session.till_mismatch");
        }

        var till = await _dbContext.Tills
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == command.TillId,
                cancellationToken);

        if (till is null)
        {
            return OpenFailure("till_session.till_not_found");
        }

        if (!string.Equals(till.Status, TillConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return OpenFailure("till_session.till_inactive");
        }

        var existingSession = await FindOpenSessionAsync(tenantId, command.TillId, cancellationToken);
        if (existingSession is not null)
        {
            _logger.LogDebug(
                "Till open rejected: till {TillId} already has open session {SessionId}.",
                command.TillId,
                existingSession.Id);
            return OpenFailure("till_session.already_open");
        }

        var sessionNumber = await _codeSequenceRepository.GetNextCodeAsync(
            tenantId,
            "TILL_SESSION_NUMBER",
            TillSessionNumberPrefix,
            TillSessionNumberPadding,
            now,
            cancellationToken);

        var session = TillSession.Open(
            Guid.NewGuid(),
            tenantId,
            till.OutletId,
            till.Id,
            sessionNumber,
            DateOnly.FromDateTime(now.UtcDateTime),
            tenantUserId,
            command.DeviceId,
            command.OpeningFloat,
            till.CurrencyCode,
            command.OpeningNote,
            now);

        _dbContext.TillSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Till opened for tenant {TenantId}: session {SessionId}, till {TillId}, device {DeviceId}.",
            tenantId,
            session.Id,
            till.Id,
            command.DeviceId);

        return OpenSuccess(MapSnapshot(session));
    }

    public async Task<CloseTillRepositoryResult> CloseTillAsync(
        Guid tenantId,
        Guid tenantUserId,
        CloseTillCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (command.CountedCash < 0)
        {
            return CloseFailure("till_session.invalid_counted_cash");
        }

        var deviceContext = await ResolveTrustedDeviceAssignmentAsync(tenantId, command.DeviceId, cancellationToken);
        if (!deviceContext.IsSuccess || deviceContext.Assignment is null)
        {
            return CloseFailure(deviceContext.ErrorCode!);
        }

        if (deviceContext.Assignment.TillId != command.TillId)
        {
            _logger.LogDebug(
                "Till close rejected: device {DeviceId} assigned to till {AssignedTillId}, requested {RequestedTillId}.",
                command.DeviceId,
                deviceContext.Assignment.TillId,
                command.TillId);
            return CloseFailure("till_session.till_mismatch");
        }

        var session = await FindOpenSessionAsync(tenantId, command.TillId, cancellationToken);
        if (session is null)
        {
            _logger.LogDebug(
                "Till close rejected: no open session for till {TillId}.",
                command.TillId);
            return CloseFailure("till_session.not_open");
        }

        var expectedCash = command.ExpectedCash ?? session.OpeningFloatAmount;
        if (expectedCash < 0)
        {
            return CloseFailure("till_session.invalid_expected_cash");
        }

        var cashDifference = command.CountedCash - expectedCash;
        if (cashDifference != 0 && string.IsNullOrWhiteSpace(command.MismatchReason))
        {
            return CloseFailure("till_session.mismatch_reason_required");
        }

        var closingNote = BuildClosingNote(command.MismatchReason, command.ClosingNote);
        session.Close(tenantUserId, command.DeviceId, closingNote, now);

        var closedEvent = TillSessionEvent.RecordClosed(
            Guid.NewGuid(),
            tenantId,
            session.Id,
            tenantUserId,
            command.DeviceId,
            command.CountedCash,
            session.CurrencyCode,
            closingNote,
            now);

        _dbContext.TillSessionEvents.Add(closedEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Till closed for tenant {TenantId}: session {SessionId}, till {TillId}, counted {CountedCash}.",
            tenantId,
            session.Id,
            command.TillId,
            command.CountedCash);

        return CloseSuccess(new ClosedTillSessionDbSnapshot(
            SessionId: session.Id,
            OutletId: session.OutletId,
            TillId: session.TillId,
            OpeningFloat: session.OpeningFloatAmount,
            ExpectedCash: expectedCash,
            CountedCash: command.CountedCash,
            CashDifference: cashDifference,
            Status: session.Status,
            OpenedAt: session.OpenedAt,
            ClosedAt: session.ClosedAt ?? now,
            ClosingNote: session.ClosingNote));
    }

    private static string? BuildClosingNote(string? mismatchReason, string? closingNote)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(mismatchReason))
        {
            parts.Add($"Mismatch: {mismatchReason.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(closingNote))
        {
            parts.Add(closingNote.Trim());
        }

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private async Task<DeviceAssignmentContextResult> ResolveTrustedDeviceAssignmentAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var device = await _dbContext.PosDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == deviceId,
                cancellationToken);

        if (device is null)
        {
            _logger.LogDebug(
                "Till session context unresolved: device {DeviceId} not found for tenant {TenantId}.",
                deviceId,
                tenantId);
            return DeviceAssignmentContextResult.Failure("till_session.device_not_found");
        }

        if (!string.Equals(device.Status, PosDeviceConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase) ||
            !device.IsTrusted)
        {
            _logger.LogDebug(
                "Till session context unresolved: device {DeviceId} is not active/trusted.",
                deviceId);
            return DeviceAssignmentContextResult.Failure("till_session.device_not_trusted");
        }

        var assignment = await (
                from row in _dbContext.TillDeviceAssignments.AsNoTracking()
                join till in _dbContext.Tills.AsNoTracking()
                    on row.TillId equals till.Id
                where row.TenantId == tenantId &&
                      row.PosDeviceId == deviceId &&
                      row.ReleasedAt == null &&
                      till.TenantId == tenantId &&
                      till.Status == TillConstants.ActiveStatus
                orderby row.AssignedAt descending
                select new DeviceAssignmentSnapshot(row.TillId, till.OutletId))
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            _logger.LogDebug(
                "Till session context unresolved: no active till assignment for device {DeviceId}.",
                deviceId);
            return DeviceAssignmentContextResult.Failure("till_session.till_not_assigned");
        }

        return DeviceAssignmentContextResult.Success(assignment);
    }

    private async Task<TillSession?> FindOpenSessionAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken) =>
        await _dbContext.TillSessions
            .Where(x =>
                x.TenantId == tenantId &&
                x.TillId == tillId &&
                x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static CurrentTillSessionDbSnapshot MapSnapshot(TillSession session) =>
        new(
            SessionId: session.Id,
            OutletId: session.OutletId,
            TillId: session.TillId,
            OpenedDeviceId: session.OpenedFromPosDeviceId,
            OpeningFloat: session.OpeningFloatAmount,
            Status: session.Status,
            OpenedAt: session.OpenedAt,
            OpeningNote: session.OpeningNote);

    private static CurrentTillSessionResolveResult ResolveSuccess(CurrentTillSessionDbSnapshot snapshot) =>
        new(true, null, snapshot);

    private static CurrentTillSessionResolveResult ResolveFailure(string errorCode) =>
        new(false, errorCode, null);

    private static OpenTillRepositoryResult OpenSuccess(CurrentTillSessionDbSnapshot snapshot) =>
        new(true, null, snapshot);

    private static OpenTillRepositoryResult OpenFailure(string errorCode) =>
        new(false, errorCode, null);

    private static CloseTillRepositoryResult CloseSuccess(ClosedTillSessionDbSnapshot snapshot) =>
        new(true, null, snapshot);

    private static CloseTillRepositoryResult CloseFailure(string errorCode) =>
        new(false, errorCode, null);

    private sealed record DeviceAssignmentSnapshot(Guid TillId, Guid OutletId);

    private sealed record DeviceAssignmentContextResult(bool IsSuccess, string? ErrorCode, DeviceAssignmentSnapshot? Assignment)
    {
        public static DeviceAssignmentContextResult Success(DeviceAssignmentSnapshot assignment) =>
            new(true, null, assignment);

        public static DeviceAssignmentContextResult Failure(string errorCode) =>
            new(false, errorCode, null);
    }
}
