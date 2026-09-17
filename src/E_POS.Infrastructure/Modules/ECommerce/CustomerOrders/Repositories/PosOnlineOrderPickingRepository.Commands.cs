using System.Text.Json;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed partial class PosOnlineOrderPickingRepository
{
    public Task<PosOnlineOrderPickingRepositoryResult> PickLineAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickLineRequest request, DateTimeOffset now,
        CancellationToken cancellationToken) =>
        ExecuteMutationAsync(tenantId, tenantUserId, outletId, orderId, lineId,
            request.ExpectedVersion, now, cancellationToken,
            async (aggregate, line, salesLine, sequence) =>
            {
                if (request.InputMethod is "SCAN" or "MANUAL")
                {
                    if (string.IsNullOrWhiteSpace(salesLine.BarcodeSnapshot))
                        return MutationResult.Failure("online_orders.barcode_snapshot_unavailable");

                    if (!string.Equals(
                            salesLine.BarcodeSnapshot.Trim(),
                            request.Barcode,
                            StringComparison.OrdinalIgnoreCase))
                        return MutationResult.Failure("online_orders.invalid_barcode");
                }

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
}
