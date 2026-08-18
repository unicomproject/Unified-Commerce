using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;

public sealed class PosDrawerRepository : IPosDrawerRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly EPosDbContext _db;

    public PosDrawerRepository(EPosDbContext db) => _db = db;

    public async Task<CashDrawerOperationDto?> GetOperationByIdAsync(
        Guid tenantId, Guid operationId, CancellationToken cancellationToken)
    {
        var op = await _db.CashDrawerOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == operationId, cancellationToken);
        return op is null ? null : Map(op);
    }

    public async Task<CashDrawerOperationDto?> GetOperationByRequestIdAsync(
        Guid tenantId, Guid requestId, CancellationToken cancellationToken)
    {
        var op = await _db.CashDrawerOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.RequestId == requestId, cancellationToken);
        return op is null ? null : Map(op);
    }

    public async Task<CashDrawerSettingsDto?> GetActiveDrawerSettingsAsync(
        Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        var configDevice = await GetActiveDrawerDeviceAsync(tenantId, posDeviceId, cancellationToken);
        if (configDevice is null || configDevice.Status != "ACTIVE")
            return null;

        return JsonSerializer.Deserialize<CashDrawerSettingsDto>(configDevice.ConfigJson ?? "{}", JsonOptions);
    }

    public async Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> RegisterOperationAsync(
        Guid tenantId,
        Guid userId,
        RegisterDrawerOperationRequest request,
        Guid? approverId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // 1. Request ID idempotency check
        var canonical = JsonSerializer.Serialize(request, JsonOptions);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

        var existing = await _db.CashDrawerOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.RequestId == request.RequestId, cancellationToken);

        if (existing is not null)
        {
            if (existing.PayloadHash != payloadHash)
                return ("pos_drawer.idempotency_conflict", null);

            return (null, Map(existing));
        }

        // 2. Resolve active till
        var activeTillId = request.TillId ?? await ResolveActiveTillIdAsync(tenantId, request.PosDeviceId, cancellationToken);
        if (activeTillId == Guid.Empty)
            return ("pos_drawer.till_not_assigned", null);

        // 3. Resolve active till session
        var activeSession = await ActiveSessionAsync(tenantId, request.PosDeviceId, activeTillId, cancellationToken);
        if (activeSession is null)
            return ("pos_drawer.till_session_not_open", null);

        // 4. Fetch drawer device
        var configDevice = await GetActiveDrawerDeviceAsync(tenantId, request.PosDeviceId, cancellationToken);
        if (configDevice is null || configDevice.Status != "ACTIVE")
            return ("pos_drawer.configuration_missing", null);

        var settings = JsonSerializer.Deserialize<CashDrawerSettingsDto>(configDevice.ConfigJson ?? "{}", JsonOptions);
        if (settings is null)
            return ("pos_drawer.configuration_invalid", null);

        // 5. Check purpose policy
        var purpose = request.DrawerPurpose.Trim().ToLowerInvariant();
        if (purpose == "cashsale" && !settings.OpenOnCashSale)
            return ("pos_drawer.purpose_disabled", null);
        if (purpose == "cashrefund" && !settings.OpenOnCashRefund)
            return ("pos_drawer.purpose_disabled", null);
        if (purpose == "splitpaymentcash" && !settings.OpenOnCashSplit)
            return ("pos_drawer.purpose_disabled", null);

        var op = CashDrawerOperation.Create(
            Guid.NewGuid(),
            tenantId,
            configDevice.OutletId,
            configDevice.Id,
            request.PosDeviceId,
            activeTillId,
            activeSession.Id,
            userId,
            approverId,
            request.RequestId,
            request.DrawerPurpose,
            request.Reason,
            request.BusinessReferenceType,
            request.BusinessReferenceId,
            configDevice.Id,
            configDevice.ConfigurationVersion,
            settings.DrawerPort ?? "drawerPin2",
            settings.PulseOnMilliseconds ?? 100,
            settings.PulseOffMilliseconds ?? 200,
            "Pending",
            null,
            null,
            false,
            null,
            now,
            payloadHash,
            now);

        _db.CashDrawerOperations.Add(op);
        await _db.SaveChangesAsync(cancellationToken);

        return (null, Map(op));
    }

    public async Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> FinalizeOperationAsync(
        Guid tenantId,
        Guid userId,
        Guid operationId,
        FinalizeDrawerOperationRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var op = await _db.CashDrawerOperations
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == operationId, cancellationToken);

        if (op is null)
            return ("pos_drawer.operation_not_found", null);

        if (op.Status is "OPENED" or "FAILED" or "CANCELLED")
            return ("pos_drawer.already_finalized", Map(op));

        // AGENT_ACCEPTED / UNKNOWN remain updatable for physical confirmation.
        op.FinalizeOperation(
            request.Status,
            request.ResultCategory,
            request.FailureCategory,
            request.AgentAccepted,
            request.PhysicalConfirmation,
            now);

        await _db.SaveChangesAsync(cancellationToken);
        return (null, Map(op));
    }

    public async Task<IReadOnlyList<CashDrawerOperationDto>> GetHistoryAsync(
        Guid tenantId, Guid posDeviceId, int take, CancellationToken cancellationToken)
    {
        var ops = await _db.CashDrawerOperations.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PosDeviceId == posDeviceId)
            .OrderByDescending(x => x.InitiatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return ops.Select(Map).ToList();
    }

    public async Task<PosCashDrawerSummaryDto?> GetFinancialSummaryAsync(
        Guid tenantId, Guid tillSessionId, CancellationToken cancellationToken)
    {
        var session = await (
            from item in _db.TillSessions.AsNoTracking()
            join till in _db.Tills.AsNoTracking() on item.TillId equals till.Id
            join user in _db.TenantUsers.AsNoTracking() on item.OpenedByTenantUserId equals user.Id
            where item.TenantId == tenantId && till.TenantId == tenantId && user.TenantId == tenantId &&
                  item.Id == tillSessionId && item.Status == "OPEN"
            select new
            {
                item.Id, item.TillId, till.TillName, item.Status, item.CurrencyCode,
                item.OpeningFloatAmount, item.OpenedAt,
                OpenedBy = user.DisplayName ?? user.FullName
            }).SingleOrDefaultAsync(cancellationToken);
        if (session is null) return null;

        var payments = await (
            from payment in _db.SalesPayments.AsNoTracking()
            join method in _db.PaymentMethods.AsNoTracking() on payment.PaymentMethodId equals method.Id
            where payment.TenantId == tenantId && method.TenantId == tenantId &&
                  payment.TillSessionId == tillSessionId && payment.CurrencyCode == session.CurrencyCode &&
                  method.MethodCode == "CASH" &&
                  (payment.PaymentStatus == "PAID" || payment.PaymentStatus == "PARTIALLY_REFUNDED" || payment.PaymentStatus == "REFUNDED")
            group payment by 1 into grouped
            select new { Sales = grouped.Sum(x => x.PaidAmount), Refunds = grouped.Sum(x => x.RefundedAmount) })
            .SingleOrDefaultAsync(cancellationToken);

        var manual = await (
            from movement in _db.CashMovements.AsNoTracking()
            join type in _db.CashMovementTypes.AsNoTracking() on movement.MovementTypeId equals type.Id
            where movement.TenantId == tenantId && movement.TillSessionId == tillSessionId &&
                  movement.CurrencyCode == session.CurrencyCode && type.AffectsExpectedCash
            select new { movement.Amount, type.Direction, type.MovementTypeCode })
            .GroupBy(_ => 1)
            .Select(grouped => new
            {
                CashIn = grouped.Where(x => x.Direction == "IN").Sum(x => x.Amount),
                CashOut = grouped.Where(x => x.Direction == "OUT" && x.MovementTypeCode != "CASH_DROP").Sum(x => x.Amount),
                CashDrops = grouped.Where(x => x.Direction == "OUT" && x.MovementTypeCode == "CASH_DROP").Sum(x => x.Amount)
            }).SingleOrDefaultAsync(cancellationToken);

        var cashSales = payments?.Sales ?? 0;
        var cashRefunds = payments?.Refunds ?? 0;
        var cashIn = manual?.CashIn ?? 0;
        var cashOut = manual?.CashOut ?? 0;
        var cashDrops = manual?.CashDrops ?? 0;
        var expected = session.OpeningFloatAmount + cashSales - cashRefunds + cashIn - cashOut - cashDrops;
        return new PosCashDrawerSummaryDto(
            session.Id, session.TillId, session.TillName, session.Status, session.CurrencyCode,
            session.OpeningFloatAmount, cashSales, cashRefunds, cashIn, cashOut, cashDrops,
            expected, session.OpenedBy, session.OpenedAt);
    }

    public async Task<PosCashDrawerMovementPageDto> GetFinancialMovementsAsync(
        Guid tenantId, Guid tillSessionId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var take = checked(page * pageSize);
        var payments = await (
            from payment in _db.SalesPayments.AsNoTracking()
            join method in _db.PaymentMethods.AsNoTracking() on payment.PaymentMethodId equals method.Id
            join user in _db.TenantUsers.AsNoTracking() on payment.CreatedByTenantUserId equals (Guid?)user.Id into users
            from user in users.DefaultIfEmpty()
            where payment.TenantId == tenantId && method.TenantId == tenantId &&
                  payment.TillSessionId == tillSessionId && method.MethodCode == "CASH" &&
                  (payment.PaymentStatus == "PAID" || payment.PaymentStatus == "PARTIALLY_REFUNDED" || payment.PaymentStatus == "REFUNDED")
            orderby payment.PaidAt descending
            select new PosCashDrawerMovementDto(payment.Id, "CASH_SALE", "IN", payment.PaidAmount,
                payment.CurrencyCode, null, payment.PaymentNumber,
                user == null ? "System" : user.DisplayName ?? user.FullName,
                payment.PaidAt ?? payment.InitiatedAt)).Take(take).ToListAsync(cancellationToken);

        var refunds = await (
            from payment in _db.SalesPayments.AsNoTracking()
            join method in _db.PaymentMethods.AsNoTracking() on payment.PaymentMethodId equals method.Id
            join user in _db.TenantUsers.AsNoTracking() on payment.UpdatedByTenantUserId equals (Guid?)user.Id into users
            from user in users.DefaultIfEmpty()
            where payment.TenantId == tenantId && method.TenantId == tenantId && method.MethodCode == "CASH" &&
                  payment.TillSessionId == tillSessionId && payment.RefundedAmount > 0
            orderby payment.UpdatedAt descending
            select new PosCashDrawerMovementDto(payment.Id, "CASH_REFUND", "OUT", payment.RefundedAmount,
                payment.CurrencyCode, "Cash refund", payment.PaymentNumber,
                user == null ? "System" : user.DisplayName ?? user.FullName, payment.UpdatedAt ?? payment.InitiatedAt)).Take(take).ToListAsync(cancellationToken);

        var manual = await (
            from movement in _db.CashMovements.AsNoTracking()
            join type in _db.CashMovementTypes.AsNoTracking() on movement.MovementTypeId equals type.Id
            join user in _db.TenantUsers.AsNoTracking() on movement.PerformedByTenantUserId equals user.Id
            where movement.TenantId == tenantId && movement.TillSessionId == tillSessionId &&
                  (type.TenantId == null || type.TenantId == tenantId)
            orderby movement.PerformedAt descending
            select new PosCashDrawerMovementDto(movement.Id, type.MovementTypeCode,
                type.Direction, movement.Amount, movement.CurrencyCode, movement.Reason, movement.MovementNumber,
                user.DisplayName ?? user.FullName, movement.PerformedAt, movement.MovementNumber,
                type.Id, type.MovementTypeName)).Take(take).ToListAsync(cancellationToken);

        var paymentCount = await _db.SalesPayments.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TillSessionId == tillSessionId &&
                        (x.PaymentStatus == "PAID" || x.PaymentStatus == "PARTIALLY_REFUNDED" || x.PaymentStatus == "REFUNDED"))
            .Join(_db.PaymentMethods.AsNoTracking().Where(x => x.TenantId == tenantId && x.MethodCode == "CASH"),
                payment => payment.PaymentMethodId, method => method.Id, (payment, _) => payment)
            .CountAsync(cancellationToken);
        var refundCount = await _db.SalesPayments.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TillSessionId == tillSessionId && x.RefundedAmount > 0)
            .Join(_db.PaymentMethods.AsNoTracking().Where(x => x.TenantId == tenantId && x.MethodCode == "CASH"),
                payment => payment.PaymentMethodId, method => method.Id, (payment, _) => payment)
            .CountAsync(cancellationToken);
        var manualCount = await _db.CashMovements.AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.TillSessionId == tillSessionId, cancellationToken);
        var total = paymentCount + refundCount + manualCount;
        var items = payments.Concat(refunds).Concat(manual)
            .OrderByDescending(x => x.PerformedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PosCashDrawerMovementPageDto(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<IReadOnlyList<PosCashMovementTypeDto>> GetMovementTypesAsync(
        Guid tenantId, string direction, CancellationToken cancellationToken)
    {
        var normalizedDirection = direction.Trim().ToUpperInvariant();
        return await _db.CashMovementTypes.AsNoTracking()
            .Where(x => (x.TenantId == null || x.TenantId == tenantId) &&
                        x.Direction == normalizedDirection && x.Status == "ACTIVE")
            .OrderBy(x => x.TenantId == null ? 0 : 1)
            .ThenBy(x => x.MovementTypeName)
            .Select(x => new PosCashMovementTypeDto(
                x.Id, x.MovementTypeCode, x.MovementTypeName, x.Direction,
                x.RequiresReason, x.AffectsExpectedCash))
            .ToListAsync(cancellationToken);
    }

    public Task<(string? ErrorCode, PosCashDrawerMovementDto? Movement)> CreateFinancialMovementAsync(
        Guid tenantId, Guid userId, Guid trustedTillId, CreatePosCashMovementRequest request,
        DateTimeOffset now, CancellationToken cancellationToken) =>
        CreateFinancialMovementCoreAsync(
            tenantId, userId, trustedTillId, request, now, allowSerializationRetry: true, cancellationToken);

    private async Task<(string? ErrorCode, PosCashDrawerMovementDto? Movement)> CreateFinancialMovementCoreAsync(
        Guid tenantId, Guid userId, Guid trustedTillId, CreatePosCashMovementRequest request,
        DateTimeOffset now, bool allowSerializationRetry, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        var committed = false;
        try
        {
            if (_db.Database.IsRelational())
            {
                transaction = await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);
            }

            var existing = await _db.CashMovements.AsNoTracking()
                .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.RequestId == request.RequestId, cancellationToken);
            if (existing is not null)
            {
                var same = existing.PosDeviceId == request.DeviceId &&
                           existing.MovementTypeId == request.MovementTypeId &&
                           existing.Amount == request.Amount &&
                           existing.Reason == NormalizeNote(request.Note);
                return same ? (null, await MapCanonicalAsync(existing, cancellationToken)) : ("cash_drawer.idempotency_conflict", null);
            }

            var session = await _db.TillSessions.SingleOrDefaultAsync(x => x.TenantId == tenantId &&
                x.TillId == trustedTillId && x.OpenedFromPosDeviceId == request.DeviceId && x.Status == "OPEN", cancellationToken);
            if (session is null) return ("cash_drawer.till_session_not_open", null);

            var movementType = await _db.CashMovementTypes.AsNoTracking().SingleOrDefaultAsync(x =>
                x.Id == request.MovementTypeId && (x.TenantId == null || x.TenantId == tenantId) &&
                x.Status == "ACTIVE", cancellationToken);
            if (movementType is null) return ("cash_drawer.movement_type_not_found", null);
            if (movementType.Direction is not ("IN" or "OUT"))
                return ("cash_drawer.movement_type_invalid_direction", null);

            var note = NormalizeNote(request.Note);
            if (movementType.RequiresReason && note is null) return ("cash_drawer.reason_required", null);

            if (movementType.Direction == "OUT" && movementType.AffectsExpectedCash)
            {
                var available = await GetFinancialSummaryAsync(tenantId, session.Id, cancellationToken);
                if (available is null) return ("cash_drawer.till_session_not_open", null);
                if (request.Amount > available.CurrentExpectedCash)
                    return ("cash_drawer.insufficient_expected_cash", null);
            }

            var movement = CashMovement.Create(
                Guid.NewGuid(), tenantId, session.OutletId, session.TillId, session.Id, request.DeviceId,
                request.RequestId, movementType.Id, $"CM-{request.RequestId:N}", request.Amount,
                session.CurrencyCode, note, null, null, null, userId, now);
            _db.CashMovements.Add(movement);
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                committed = true;
            }

            var summary = await GetFinancialSummaryAsync(tenantId, session.Id, cancellationToken);
            return (null, await MapCanonicalAsync(movement, cancellationToken, summary?.CurrentExpectedCash));
        }
        catch (Exception exception) when (IsIdempotencyRace(exception))
        {
            if (transaction is not null && !committed)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _db.ChangeTracker.Clear();
            var raced = await _db.CashMovements.AsNoTracking()
                .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.RequestId == request.RequestId, cancellationToken);
            if (raced is not null)
            {
                var same = raced.PosDeviceId == request.DeviceId &&
                           raced.MovementTypeId == request.MovementTypeId &&
                           raced.Amount == request.Amount &&
                           raced.Reason == NormalizeNote(request.Note);
                return same ? (null, await MapCanonicalAsync(raced, cancellationToken)) : ("cash_drawer.idempotency_conflict", null);
            }

            if (allowSerializationRetry && IsSerializationOrDeadlock(exception))
            {
                return await CreateFinancialMovementCoreAsync(
                    tenantId, userId, trustedTillId, request, now,
                    allowSerializationRetry: false, cancellationToken);
            }

            throw;
        }
        catch
        {
            if (transaction is not null && !committed)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private static bool IsSerializationOrDeadlock(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState is PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }

            if (current is DbUpdateException dbUpdate &&
                dbUpdate.InnerException is PostgresException innerPostgres &&
                innerPostgres.SqlState is PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<PosCashDrawerMovementDto> MapCanonicalAsync(
        CashMovement movement, CancellationToken cancellationToken, decimal? currentExpectedCash = null)
    {
        var details = await (
            from type in _db.CashMovementTypes.AsNoTracking()
            join user in _db.TenantUsers.AsNoTracking() on movement.PerformedByTenantUserId equals user.Id
            where type.Id == movement.MovementTypeId
            select new { type.MovementTypeCode, type.MovementTypeName, type.Direction, Name = user.DisplayName ?? user.FullName })
            .SingleAsync(cancellationToken);
        return new PosCashDrawerMovementDto(
            movement.Id, details.MovementTypeCode, details.Direction, movement.Amount, movement.CurrencyCode,
            movement.Reason, movement.MovementNumber, details.Name, movement.PerformedAt,
            movement.MovementNumber, movement.MovementTypeId, details.MovementTypeName, currentExpectedCash);
    }

    private static bool IsIdempotencyRace(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState is PostgresErrorCodes.UniqueViolation
                    or PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }

            if (current is DbUpdateException dbUpdate &&
                dbUpdate.InnerException is PostgresException innerPostgres &&
                innerPostgres.SqlState is PostgresErrorCodes.UniqueViolation
                    or PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeNote(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Guid> ResolveActiveTillIdAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return await _db.TillDeviceAssignments.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PosDeviceId == posDeviceId && x.ReleasedAt == null)
            .Select(x => x.TillId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TillSession?> ActiveSessionAsync(Guid tenantId, Guid posDeviceId, Guid tillId, CancellationToken cancellationToken)
    {
        return await _db.TillSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        (x.OpenedFromPosDeviceId == posDeviceId || x.TillId == tillId) &&
                        x.Status == "OPEN")
            .OrderByDescending(x => x.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<HardwareDevice?> GetActiveDrawerDeviceAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return await (
            from assignment in _db.HardwareDeviceAssignments.AsNoTracking()
            join device in _db.HardwareDevices.AsNoTracking()
                on assignment.HardwareDeviceId equals device.Id
            where assignment.TenantId == tenantId &&
                  assignment.PosDeviceId == posDeviceId &&
                  assignment.ReleasedAt == null &&
                  device.HardwareDeviceType == "CASHDRAWER" &&
                  device.Status != "DELETED"
            select device).SingleOrDefaultAsync(cancellationToken);
    }

    private static CashDrawerOperationDto Map(CashDrawerOperation op)
    {
        return new CashDrawerOperationDto(
            op.Id,
            op.TenantId,
            op.OutletId,
            op.HardwareDeviceId,
            op.PosDeviceId,
            op.TillId,
            op.TillSessionId,
            op.ProcessedByUserId,
            op.ApproverId,
            op.RequestId,
            op.DrawerPurpose,
            op.Reason,
            op.BusinessReferenceType,
            op.BusinessReferenceId,
            op.ConfigurationId,
            op.ConfigurationVersion,
            op.DrawerPort,
            op.PulseOnTime,
            op.PulseOffTime,
            op.Status,
            op.ResultCategory,
            op.FailureCategory,
            op.AgentAccepted,
            op.PhysicalConfirmation,
            op.InitiatedAt,
            op.CompletedAt);
    }
}
