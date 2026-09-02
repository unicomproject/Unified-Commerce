using System.Text.Json;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed class PosOnlineOrderPickingRepository : CustomerOrderRepositoryBase, IPosOnlineOrderPickingRepository
{
    public const string LinePickedEvent = "FULFILLMENT_LINE_PICKED";
    public const string IssueReportedEvent = "FULFILLMENT_LINE_ISSUE_REPORTED";
    public const string PickingCompletedEvent = "FULFILLMENT_PICKING_COMPLETED";
    public const string PickingNoteAddedEvent = "FULFILLMENT_PICKING_NOTE_ADDED";
    private const int PickingNoteHistoryLimit = 50;

    public PosOnlineOrderPickingRepository(
        EPosDbContext dbContext,
        IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
    }

    public async Task<PosOnlineOrderPickingRepositoryResult> GetAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
        DateTimeOffset serverTime, CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(tenantId, tenantUserId, outletId, cancellationToken);
        if (accessError is not null)
            return PosOnlineOrderPickingRepositoryResult.Failure(accessError);

        var aggregate = await LoadAggregateAsync(
            tenantId, outletId, orderId, tracked: false, cancellationToken);
        if (aggregate is null)
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.not_found");
        if (aggregate.Fulfillment.FulfillmentStatus != "PICKING")
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.invalid_state");

        var rows = await (
            from fulfillmentLine in DbContext.FulfillmentOrderLines.AsNoTracking()
            join salesLine in DbContext.SalesOrderLines.AsNoTracking()
                on new { fulfillmentLine.TenantId, Id = fulfillmentLine.SalesOrderLineId }
                equals new { salesLine.TenantId, Id = salesLine.Id }
            where fulfillmentLine.TenantId == tenantId &&
                  fulfillmentLine.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                  salesLine.SalesOrderId == orderId && salesLine.LineStatus != "CANCELLED"
            orderby salesLine.LineNumber
            select new { FulfillmentLine = fulfillmentLine, SalesLine = salesLine })
            .ToListAsync(cancellationToken);

        var imageLookup = await BuildImageLookupAsync(
            tenantId, rows.Select(x => x.SalesLine.ProductId).Distinct().ToList(), cancellationToken);
        var location = aggregate.Fulfillment.SourceInventoryLocationId.HasValue
            ? await DbContext.InventoryLocations.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.OutletId == outletId &&
                            x.Id == aggregate.Fulfillment.SourceInventoryLocationId.Value)
                .Select(x => new { x.LocationCode, x.LocationName })
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var assignedName = aggregate.Fulfillment.AssignedToTenantUserId.HasValue
            ? await DbContext.TenantUsers.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == aggregate.Fulfillment.AssignedToTenantUserId.Value)
                .Select(x => x.DisplayName ?? x.FullName)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var issuePayloads = await DbContext.FulfillmentOrderEvents.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                        x.EventType == IssueReportedEvent && x.EventPayloadJson != null)
            .Select(x => x.EventPayloadJson!)
            .ToListAsync(cancellationToken);
        var notesDescending = await (
            from noteEvent in DbContext.FulfillmentOrderEvents.AsNoTracking()
            join actor in DbContext.TenantUsers.AsNoTracking()
                on new { noteEvent.TenantId, Id = noteEvent.EventByTenantUserId!.Value }
                equals new { actor.TenantId, Id = actor.Id }
            where noteEvent.TenantId == tenantId &&
                  noteEvent.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                  noteEvent.EventType == PickingNoteAddedEvent &&
                  noteEvent.EventNote != null &&
                  noteEvent.EventByTenantUserId != null
            orderby noteEvent.SequenceNumber descending
            select new PosOnlineOrderPickingNoteResponse
            {
                Id = noteEvent.Id,
                Note = noteEvent.EventNote!,
                CreatedAt = noteEvent.EventAt,
                CreatedByTenantUserId = actor.Id,
                CreatedByDisplayName = actor.DisplayName ?? actor.FullName
            })
            .Take(PickingNoteHistoryLimit)
            .ToListAsync(cancellationToken);
        notesDescending.Reverse();

        var lines = rows.Select(row =>
        {
            var requested = row.FulfillmentLine.RequestedQuantity;
            var remaining = Math.Max(requested - row.FulfillmentLine.CancelledQuantity -
                                     row.FulfillmentLine.PickedQuantity, 0m);
            var lineIdText = row.FulfillmentLine.Id.ToString("D");
            return new PosOnlineOrderPickingLineResponse
            {
                Id = row.FulfillmentLine.Id,
                SalesOrderLineId = row.SalesLine.Id,
                ProductId = row.SalesLine.ProductId,
                ProductVariantId = row.SalesLine.ProductVariantId,
                LineNumber = row.SalesLine.LineNumber,
                ProductName = row.SalesLine.ProductNameSnapshot,
                VariantName = row.SalesLine.VariantNameSnapshot,
                Sku = row.SalesLine.SkuSnapshot,
                Barcode = row.SalesLine.BarcodeSnapshot,
                ImageUrl = imageLookup.GetValueOrDefault(row.SalesLine.ProductId),
                AltText = row.SalesLine.ProductNameSnapshot,
                LocationCode = location?.LocationCode,
                LocationName = location?.LocationName,
                RequestedQuantity = requested,
                PickedQuantity = row.FulfillmentLine.PickedQuantity,
                RemainingQuantity = remaining,
                Status = row.FulfillmentLine.LineStatus,
                HasReportedIssue = issuePayloads.Any(payload =>
                    payload.Contains(lineIdText, StringComparison.OrdinalIgnoreCase))
            };
        }).ToList();

        var pickedLines = lines.Count(x => x.RemainingQuantity == 0);
        var totalUnits = lines.Sum(x => x.RequestedQuantity);
        var pickedUnits = lines.Sum(x => x.PickedQuantity);
        return PosOnlineOrderPickingRepositoryResult.QuerySuccess(new PosOnlineOrderPickingResponse
        {
            OrderId = aggregate.Order.Id,
            OrderNumber = aggregate.Order.OrderNumber,
            FulfillmentOrderId = aggregate.Fulfillment.Id,
            FulfillmentNumber = aggregate.Fulfillment.FulfillmentNumber,
            Status = aggregate.Fulfillment.FulfillmentStatus,
            AssignedToTenantUserId = aggregate.Fulfillment.AssignedToTenantUserId,
            AssignedToName = assignedName ?? string.Empty,
            CustomerName = aggregate.Order.CustomerNameSnapshot ?? string.Empty,
            CollectionAt = aggregate.Order.RequestedCollectionAt,
            OutletId = outletId,
            OutletName = aggregate.Order.ReportingOutletNameSnapshot ?? string.Empty,
            TotalLines = lines.Count,
            PickedLines = pickedLines,
            TotalUnits = totalUnits,
            PickedUnits = pickedUnits,
            RemainingUnits = Math.Max(totalUnits - pickedUnits, 0m),
            CanPack = lines.Count > 0 && pickedLines == lines.Count,
            FulfillmentVersion = aggregate.Fulfillment.RowVersion,
            ServerTime = serverTime,
            Lines = lines,
            Notes = notesDescending
        });
    }

    public Task<PosOnlineOrderPickingRepositoryResult> PickLineAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickLineRequest request, DateTimeOffset now,
        CancellationToken cancellationToken) =>
        ExecuteMutationAsync(tenantId, tenantUserId, outletId, orderId, lineId,
            request.ExpectedVersion, now, cancellationToken,
            async (aggregate, line, salesLine, sequence) =>
            {
                if (request.InputMethod == "SCAN" &&
                    (string.IsNullOrWhiteSpace(salesLine.BarcodeSnapshot) ||
                     !string.Equals(salesLine.BarcodeSnapshot.Trim(), request.Barcode,
                         StringComparison.OrdinalIgnoreCase)))
                    return MutationResult.Failure("online_orders.invalid_barcode");

                try
                {
                    line.Pick(request.Quantity, tenantUserId, now);
                    aggregate.Fulfillment.RecordPickingMutation(tenantUserId, request.ExpectedVersion, now);
                    MarkPickingChanges(aggregate.Fulfillment, line, request.ExpectedVersion);
                }
                catch (InvalidOperationException ex) when (ex.Message is
                    "FULFILLMENT_PICK_QUANTITY_INVALID" or "FULFILLMENT_PICK_QUANTITY_EXCEEDED")
                {
                    return MutationResult.Failure("online_orders.invalid_quantity");
                }

                var payload = JsonSerializer.Serialize(new
                {
                    fulfillmentLineId = line.Id,
                    quantity = request.Quantity,
                    inputMethod = request.InputMethod,
                    pickedQuantity = line.PickedQuantity
                });
                DbContext.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Create(
                    Guid.NewGuid(), tenantId, aggregate.Fulfillment.Id, ++sequence,
                    LinePickedEvent, "PICKING", "PICKING", now, tenantUserId,
                    "Fulfilment line quantity picked", payload));

                var allLines = await DbContext.FulfillmentOrderLines
                    .Where(x => x.TenantId == tenantId &&
                                x.FulfillmentOrderId == aggregate.Fulfillment.Id)
                    .ToListAsync(cancellationToken);
                var canPack = allLines.Count > 0 && allLines.All(x =>
                    x.PickedQuantity + x.CancelledQuantity >= x.RequestedQuantity);
                if (canPack)
                {
                    DbContext.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Create(
                        Guid.NewGuid(), tenantId, aggregate.Fulfillment.Id, ++sequence,
                        PickingCompletedEvent, "PICKING", "PICKING", now, tenantUserId,
                        "All required fulfilment quantities picked"));
                }

                return MutationResult.Success(canPack);
            });

    public Task<PosOnlineOrderPickingRepositoryResult> ReportIssueAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickingIssueRequest request, DateTimeOffset now,
        CancellationToken cancellationToken) =>
        ExecuteMutationAsync(tenantId, tenantUserId, outletId, orderId, lineId,
            request.ExpectedVersion, now, cancellationToken,
            (aggregate, line, _, sequence) =>
            {
                aggregate.Fulfillment.RecordPickingMutation(tenantUserId, request.ExpectedVersion, now);
                MarkFulfillmentMutation(aggregate.Fulfillment, request.ExpectedVersion);
                var payload = JsonSerializer.Serialize(new
                {
                    fulfillmentLineId = line.Id,
                    reason = request.Reason,
                    note = request.Note
                });
                DbContext.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Create(
                    Guid.NewGuid(), tenantId, aggregate.Fulfillment.Id, sequence + 1,
                    IssueReportedEvent, "PICKING", "PICKING", now, tenantUserId,
                    request.Reason, payload));
                return Task.FromResult(MutationResult.Success(false));
            });

    public async Task<PosOnlineOrderPickingRepositoryResult> AddNoteAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
        PosOnlineOrderPickingNoteRequest request, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        if (DbContext.Database.IsRelational())
            transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var accessError = await ValidateAccessAsync(tenantId, tenantUserId, outletId, cancellationToken);
            if (accessError is not null)
                return await RollbackFailureAsync(transaction, accessError, cancellationToken);

            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, tracked: true, cancellationToken);
            if (aggregate is null)
                return await RollbackFailureAsync(transaction, "online_orders.not_found", cancellationToken);
            if (aggregate.Fulfillment.FulfillmentStatus != "PICKING")
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);
            if (request.ExpectedVersion <= 0 || aggregate.Fulfillment.RowVersion != request.ExpectedVersion)
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);

            var actorName = await DbContext.TenantUsers
                .Where(x => x.TenantId == tenantId && x.Id == tenantUserId)
                .Select(x => x.DisplayName ?? x.FullName)
                .FirstAsync(cancellationToken);
            var sequence = await DbContext.FulfillmentOrderEvents
                .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == aggregate.Fulfillment.Id)
                .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;

            aggregate.Fulfillment.AddPickingNote(tenantUserId, request.ExpectedVersion, now);
            MarkFulfillmentMutation(aggregate.Fulfillment, request.ExpectedVersion);
            var noteEvent = FulfillmentOrderEvent.Create(
                Guid.NewGuid(), tenantId, aggregate.Fulfillment.Id, sequence + 1,
                PickingNoteAddedEvent, "PICKING", "PICKING", now, tenantUserId,
                request.Note);
            DbContext.FulfillmentOrderEvents.Add(noteEvent);

            await DbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return PosOnlineOrderPickingRepositoryResult.NoteSuccess(
                new PosOnlineOrderPickingNoteCommandResponse
                {
                    OrderId = aggregate.Order.Id,
                    FulfillmentOrderId = aggregate.Fulfillment.Id,
                    FulfillmentVersion = aggregate.Fulfillment.RowVersion,
                    Note = new PosOnlineOrderPickingNoteResponse
                    {
                        Id = noteEvent.Id,
                        Note = noteEvent.EventNote!,
                        CreatedAt = noteEvent.EventAt,
                        CreatedByTenantUserId = tenantUserId,
                        CreatedByDisplayName = actorName
                    }
                });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.concurrency_conflict");
        }
        catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_VERSION_CONFLICT")
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.concurrency_conflict");
        }
        catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_NOT_PICKABLE")
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.invalid_state");
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task<PosOnlineOrderPickingRepositoryResult> ExecuteMutationAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
        long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken,
        Func<PickingAggregate, FulfillmentOrderLine,
            E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderLine, int,
            Task<MutationResult>> mutate)
    {
        IDbContextTransaction? transaction = null;
        if (DbContext.Database.IsRelational())
            transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var accessError = await ValidateAccessAsync(tenantId, tenantUserId, outletId, cancellationToken);
            if (accessError is not null)
                return await RollbackFailureAsync(transaction, accessError, cancellationToken);

            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, tracked: true, cancellationToken);
            if (aggregate is null)
                return await RollbackFailureAsync(transaction, "online_orders.not_found", cancellationToken);
            if (aggregate.Fulfillment.FulfillmentStatus != "PICKING")
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);
            if (expectedVersion <= 0 || aggregate.Fulfillment.RowVersion != expectedVersion)
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);

            var lineRow = await (
                from fulfillmentLine in DbContext.FulfillmentOrderLines
                join salesLine in DbContext.SalesOrderLines
                    on new { fulfillmentLine.TenantId, Id = fulfillmentLine.SalesOrderLineId }
                    equals new { salesLine.TenantId, Id = salesLine.Id }
                where fulfillmentLine.TenantId == tenantId && fulfillmentLine.Id == lineId &&
                      fulfillmentLine.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                      salesLine.SalesOrderId == orderId && salesLine.LineStatus != "CANCELLED"
                select new { FulfillmentLine = fulfillmentLine, SalesLine = salesLine })
                .FirstOrDefaultAsync(cancellationToken);
            if (lineRow is null)
                return await RollbackFailureAsync(transaction, "online_orders.invalid_line", cancellationToken);

            var sequence = await DbContext.FulfillmentOrderEvents
                .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == aggregate.Fulfillment.Id)
                .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;
            var mutation = await mutate(aggregate, lineRow.FulfillmentLine, lineRow.SalesLine, sequence);
            if (!mutation.IsSuccess)
                return await RollbackFailureAsync(transaction, mutation.ErrorCode!, cancellationToken);

            await DbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            var totalLines = await DbContext.FulfillmentOrderLines.AsNoTracking()
                .CountAsync(x => x.TenantId == tenantId &&
                                 x.FulfillmentOrderId == aggregate.Fulfillment.Id, cancellationToken);
            var completedLines = await DbContext.FulfillmentOrderLines.AsNoTracking()
                .CountAsync(x => x.TenantId == tenantId &&
                                 x.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                                 x.PickedQuantity + x.CancelledQuantity >= x.RequestedQuantity,
                    cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.CommandSuccess(
                new PosOnlineOrderPickingCommandResponse
                {
                    OrderId = aggregate.Order.Id,
                    FulfillmentOrderId = aggregate.Fulfillment.Id,
                    Status = aggregate.Fulfillment.FulfillmentStatus,
                    TotalLines = totalLines,
                    CompletedLines = completedLines,
                    CanPack = totalLines > 0 && completedLines == totalLines,
                    FulfillmentVersion = aggregate.Fulfillment.RowVersion,
                    UpdatedAt = now
                });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.concurrency_conflict");
        }
        catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_VERSION_CONFLICT")
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.concurrency_conflict");
        }
        catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_NOT_PICKABLE")
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.invalid_state");
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task<PickingAggregate?> LoadAggregateAsync(
        Guid tenantId, Guid outletId, Guid orderId, bool tracked,
        CancellationToken cancellationToken)
    {
        var orderQuery = DbContext.SalesOrders.Where(x =>
            x.TenantId == tenantId && x.Id == orderId &&
            x.OrderType == ClickAndCollectOrderType && x.ReportingOutletId == outletId);
        var order = tracked
            ? await orderQuery.FirstOrDefaultAsync(cancellationToken)
            : await orderQuery.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (order is null)
            return null;

        var eligibleFulfillmentIds = DbContext.FulfillmentMethodOutlets.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OutletId == outletId)
            .Select(x => x.Id);
        var fulfillmentQuery = DbContext.FulfillmentOrders
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == orderId &&
                        eligibleFulfillmentIds.Contains(x.FulfillmentMethodOutletId))
            .OrderByDescending(x => x.CreatedAt);
        var fulfillment = tracked
            ? await fulfillmentQuery.FirstOrDefaultAsync(cancellationToken)
            : await fulfillmentQuery.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return fulfillment is null ? null : new PickingAggregate(order, fulfillment);
    }

    private void MarkPickingChanges(
        FulfillmentOrder fulfillment, FulfillmentOrderLine line, long expectedVersion)
    {
        MarkFulfillmentMutation(fulfillment, expectedVersion);
        var entry = DbContext.Entry(line);
        if (entry.State == EntityState.Detached)
            DbContext.Attach(line);
        entry.Property(x => x.PickedQuantity).IsModified = true;
        entry.Property(x => x.PickedByTenantUserId).IsModified = true;
        entry.Property(x => x.LineStatus).IsModified = true;
        entry.Property(x => x.UpdatedAt).IsModified = true;
    }

    private void MarkFulfillmentMutation(FulfillmentOrder fulfillment, long expectedVersion)
    {
        var entry = DbContext.Entry(fulfillment);
        if (entry.State == EntityState.Detached)
            DbContext.Attach(fulfillment);
        entry.Property(x => x.RowVersion).OriginalValue = expectedVersion;
        entry.Property(x => x.RowVersion).IsModified = true;
        entry.Property(x => x.UpdatedByTenantUserId).IsModified = true;
        entry.Property(x => x.UpdatedAt).IsModified = true;
    }

    private async Task<string?> ValidateAccessAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId,
        CancellationToken cancellationToken)
    {
        var contextIsActive = await (
            from tenant in DbContext.Tenants.AsNoTracking()
            join user in DbContext.TenantUsers.AsNoTracking() on tenant.Id equals user.TenantId
            join outlet in DbContext.Outlets.AsNoTracking() on tenant.Id equals outlet.TenantId
            where tenant.Id == tenantId && tenant.Status == TenantStatusConstants.Active &&
                  user.Id == tenantUserId && user.AccountStatus == TenantUserConstants.StatusActive &&
                  outlet.Id == outletId && outlet.Status == OutletConstants.ActiveStatus
            select outlet.Id).AnyAsync(cancellationToken);
        if (!contextIsActive)
            return "online_orders.outlet_access_denied";

        var scopedOutletIds = DbContext.OutletUserRoles.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
            .Select(x => x.OutletId)
            .Union(DbContext.OutletUserPermissions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
                .Select(x => x.OutletId));
        var hasScope = await scopedOutletIds.AnyAsync(cancellationToken);
        return hasScope && !await scopedOutletIds.ContainsAsync(outletId, cancellationToken)
            ? "online_orders.outlet_access_denied"
            : null;
    }

    private static async Task<PosOnlineOrderPickingRepositoryResult> RollbackFailureAsync(
        IDbContextTransaction? transaction, string errorCode, CancellationToken cancellationToken)
    {
        if (transaction is not null)
            await transaction.RollbackAsync(cancellationToken);
        return PosOnlineOrderPickingRepositoryResult.Failure(errorCode);
    }

    private sealed record PickingAggregate(
        E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder Order,
        FulfillmentOrder Fulfillment);

    private sealed record MutationResult(bool IsSuccess, bool CanPack, string? ErrorCode)
    {
        public static MutationResult Success(bool canPack) => new(true, canPack, null);
        public static MutationResult Failure(string errorCode) => new(false, false, errorCode);
    }
}
