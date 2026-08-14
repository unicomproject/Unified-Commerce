using System.Data;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;

public sealed class PosTillSessionRepository : IPosTillSessionRepository
{
    private const string TillSessionNumberPrefix = "TS-";
    private const int TillSessionNumberPadding = 4;
    private static readonly HashSet<string> ApprovedMismatchReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "CASH_HANDLING_MISMATCH",
        "COUNTING_ERROR",
        "CASH_MISSING",
        "CASH_OVER",
        "OTHER"
    };

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

        var closeSummary = await CalculateExpectedCashAsync(tenantId, session, cancellationToken);
        var tillName = await _dbContext.Tills.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == session.TillId)
            .Select(x => x.TillName)
            .SingleOrDefaultAsync(cancellationToken);
        var openedByName = await _dbContext.TenantUsers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == session.OpenedByTenantUserId)
            .Select(x => x.DisplayName ?? x.FullName)
            .SingleOrDefaultAsync(cancellationToken);

        return ResolveSuccess(MapSnapshot(session, closeSummary.ExpectedCash, tillName, openedByName));
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

        var normalizedReason = NormalizeMismatchReason(command.MismatchReason);
        if (command.ClosingNote?.Trim().Length > 500)
        {
            return CloseFailure("till_session.closing_note_too_long");
        }

        if (normalizedReason is not null && !ApprovedMismatchReasons.Contains(normalizedReason))
        {
            return CloseFailure("till_session.invalid_mismatch_reason");
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

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            var session = await FindOpenSessionAsync(tenantId, command.TillId, cancellationToken);
            if (session is null)
            {
                _logger.LogDebug(
                    "Till close rejected: no open session for till {TillId}.",
                    command.TillId);
                return CloseFailure("till_session.already_closed");
            }

            if (await _dbContext.CashReconciliations.AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.TillSessionId == session.Id, cancellationToken))
            {
                return CloseFailure("till_session.already_closed");
            }

            var calculation = await CalculateExpectedCashAsync(tenantId, session, cancellationToken);
            var expectedCash = calculation.ExpectedCash;
            if (expectedCash < 0)
            {
                return CloseFailure("till_session.invalid_expected_cash");
            }

            var cashDifference = command.CountedCash - expectedCash;
            if (cashDifference != 0 && normalizedReason is null)
            {
                return CloseFailure("till_session.mismatch_reason_required");
            }

            var closingNote = BuildClosingNote(normalizedReason, command.ClosingNote);
            var reconciliation = CashReconciliation.Create(
                Guid.NewGuid(),
                tenantId,
                session.Id,
                $"REC-{session.SessionNumber}",
                expectedCash,
                command.CountedCash,
                cashDifference,
                session.CurrencyCode,
                cashDifference == 0 ? null : normalizedReason,
                JsonSerializer.Serialize(calculation),
                now);
            reconciliation.Submit(tenantUserId, now);
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

            _dbContext.CashReconciliations.Add(reconciliation);
            _dbContext.TillSessionEvents.Add(closedEvent);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Till close committed for tenant {TenantId}, session {SessionId}, till {TillId}, device {DeviceId}, difference {Difference}.",
                tenantId, session.Id, command.TillId, command.DeviceId, cashDifference);

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
        catch (Exception exception) when (IsConcurrentClose(exception))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            _dbContext.ChangeTracker.Clear();
            _logger.LogWarning(exception,
                "Concurrent till close rejected for tenant {TenantId}, till {TillId}, device {DeviceId}.",
                tenantId, command.TillId, command.DeviceId);
            return CloseFailure("till_session.already_closed");
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<ExpectedCashCalculation> CalculateExpectedCashAsync(
        Guid tenantId,
        TillSession session,
        CancellationToken cancellationToken)
    {
        var cashPayments = await (
            from payment in _dbContext.SalesPayments.AsNoTracking()
            join method in _dbContext.PaymentMethods.AsNoTracking()
                on payment.PaymentMethodId equals method.Id
            where payment.TenantId == tenantId &&
                  method.TenantId == tenantId &&
                  payment.TillSessionId == session.Id &&
                  payment.CurrencyCode == session.CurrencyCode &&
                  method.MethodCode == "CASH" &&
                  (payment.PaymentStatus == "PAID" ||
                   payment.PaymentStatus == "PARTIALLY_REFUNDED" ||
                   payment.PaymentStatus == "REFUNDED")
            select new { payment.PaymentNumber, payment.PaidAmount, payment.RefundedAmount })
            .ToListAsync(cancellationToken);

        var paymentReferences = cashPayments.Select(x => x.PaymentNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var movements = await _dbContext.TillCashMovements.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.TillSessionId == session.Id &&
                        x.CurrencyCode == session.CurrencyCode)
            .ToListAsync(cancellationToken);
        var configuredTypes = await _dbContext.CashMovementTypes.AsNoTracking()
            .Where(x => (x.TenantId == null || x.TenantId == tenantId) && x.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        bool AffectsExpectedCash(string code)
        {
            var configured = configuredTypes
                .Where(x => string.Equals(x.MovementTypeCode, code, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.TenantId == tenantId)
                .FirstOrDefault();
            return configured?.AffectsExpectedCash ??
                   code is "CASH_IN" or "CASH_OUT" or "CASH_DROP" or "OPENING_FLOAT" or "CLOSING_REMOVE";
        }

        var includedMovements = movements
            .Where(x => AffectsExpectedCash(x.MovementType))
            .Where(x => !(x.MovementType == "CASH_IN" &&
                          x.ReferenceNumber is not null &&
                          paymentReferences.Contains(x.ReferenceNumber)))
            .ToList();
        var cashPaymentsTotal = cashPayments.Sum(x => x.PaidAmount);
        var cashRefundsTotal = cashPayments.Sum(x => x.RefundedAmount);
        var cashIn = includedMovements.Where(x => x.MovementType == "CASH_IN").Sum(x => x.Amount);
        var cashOut = includedMovements.Where(x => x.MovementType == "CASH_OUT").Sum(x => x.Amount);
        var cashDrops = includedMovements.Where(x => x.MovementType == "CASH_DROP").Sum(x => x.Amount);
        var openingAdjustments = includedMovements.Where(x => x.MovementType == "OPENING_FLOAT").Sum(x => x.Amount);
        var closingRemovals = includedMovements.Where(x => x.MovementType == "CLOSING_REMOVE").Sum(x => x.Amount);

        return new ExpectedCashCalculation(
            Version: 1,
            CurrencyCode: session.CurrencyCode,
            OpeningFloat: session.OpeningFloatAmount,
            CashPayments: cashPaymentsTotal,
            CashIn: cashIn,
            CashOut: cashOut,
            OpeningAdjustments: openingAdjustments,
            ClosingRemovals: closingRemovals,
            ExpectedCash: session.OpeningFloatAmount + cashPaymentsTotal - cashRefundsTotal + cashIn + openingAdjustments - cashOut - cashDrops - closingRemovals);
    }

    private static string? NormalizeMismatchReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim().Replace(' ', '_').ToUpperInvariant();

    private static bool IsConcurrentClose(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState is PostgresErrorCodes.UniqueViolation or
                    PostgresErrorCodes.SerializationFailure or
                    PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }
        }

        return false;
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

    private static CurrentTillSessionDbSnapshot MapSnapshot(
        TillSession session,
        decimal expectedCash = 0,
        string? tillName = null,
        string? openedByName = null) =>
        new(
            SessionId: session.Id,
            OutletId: session.OutletId,
            TillId: session.TillId,
            OpenedDeviceId: session.OpenedFromPosDeviceId,
            OpeningFloat: session.OpeningFloatAmount,
            Status: session.Status,
            OpenedAt: session.OpenedAt,
            OpeningNote: session.OpeningNote,
            CurrencyCode: session.CurrencyCode,
            ExpectedCash: expectedCash,
            TillName: tillName,
            OpenedByName: openedByName);

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

    private sealed record ExpectedCashCalculation(
        int Version,
        string CurrencyCode,
        decimal OpeningFloat,
        decimal CashPayments,
        decimal CashIn,
        decimal CashOut,
        decimal OpeningAdjustments,
        decimal ClosingRemovals,
        decimal ExpectedCash);

    private sealed record DeviceAssignmentContextResult(bool IsSuccess, string? ErrorCode, DeviceAssignmentSnapshot? Assignment)
    {
        public static DeviceAssignmentContextResult Success(DeviceAssignmentSnapshot assignment) =>
            new(true, null, assignment);

        public static DeviceAssignmentContextResult Failure(string errorCode) =>
            new(false, errorCode, null);
    }
}
