using System.Data;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Shared.Refund.Entities;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Repositories;

public sealed class PosReturnRepository : IPosReturnRepository
{
    public bool SupportsInspectionDrafts => true;
    private readonly EPosDbContext _dbContext;
    private readonly IPosSaleLinePricingCalculator _saleLinePricingCalculator;

    public PosReturnRepository(
        EPosDbContext dbContext,
        IPosSaleLinePricingCalculator saleLinePricingCalculator)
    {
        _dbContext = dbContext;
        _saleLinePricingCalculator = saleLinePricingCalculator;
    }

    public async Task<PosReturnCompleteRepositoryResult> CompleteReturnAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosReturnCompleteCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.Equals(command.ResolutionType, "EXCHANGE", StringComparison.OrdinalIgnoreCase))
        {
            return await CompleteExchangeAsync(
                tenantId,
                tenantUserId,
                command,
                now,
                cancellationToken);
        }

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

        var priorRefundCompletion = await _dbContext.SalesReturns
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.IdempotencyKey == command.IdempotencyKey)
            .Select(x => new { x.Id, x.SalesOrderId, x.OutletId })
            .FirstOrDefaultAsync(cancellationToken);
        if (priorRefundCompletion is not null)
        {
            var sameLogicalRequest =
                priorRefundCompletion.SalesOrderId == command.SaleId &&
                priorRefundCompletion.OutletId == command.OutletId;
            return sameLogicalRequest
                ? await GetCompletionAsync(
                    tenantId,
                    command.OutletId,
                    priorRefundCompletion.Id,
                    cancellationToken)
                : new PosReturnCompleteRepositoryResult(null, "idempotency_conflict");
        }

        var draft = await _dbContext.ReturnInspectionDrafts
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.OutletId == command.OutletId &&
                     x.SaleId == command.SaleId,
                cancellationToken);
        if (draft is null)
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_required");
        }

        if (string.Equals(draft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_consumed");
        }

        if (draft.IsExpired(now))
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_expired");
        }

        if (!string.Equals(draft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_not_validated");
        }

        if (draft.Version != command.ExpectedVersion)
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_conflict");
        }

        if (!string.Equals(draft.ResolutionType, "REFUND", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnCompleteRepositoryResult(null, "invalid_resolution");
        }

        if (draft.RequiresManagerApproval)
        {
            return new PosReturnCompleteRepositoryResult(null, "approval_required");
        }

        var previewResult = await PreviewCreditAsync(
            tenantId,
            command.SaleId,
            command.ReasonCode,
            command.Lines,
            now,
            cancellationToken);
        if (previewResult.Preview is null)
        {
            return new PosReturnCompleteRepositoryResult(null, previewResult.ErrorCode);
        }

        var preview = previewResult.Preview;
        if (preview.RequiresApproval)
        {
            return new PosReturnCompleteRepositoryResult(null, "approval_required");
        }

        var reason = await _dbContext.ReturnReasons
            .FirstAsync(
                x => x.TenantId == tenantId &&
                     x.ReasonCode == command.ReasonCode &&
                     x.IsActive &&
                     (x.AppliesTo == "RETURN" || x.AppliesTo == "BOTH"),
                cancellationToken);
        var order = await _dbContext.SalesOrders
            .FirstAsync(
                x => x.TenantId == tenantId && x.Id == command.SaleId,
                cancellationToken);
        var selectedLineIds = command.Lines.Select(x => x.SaleLineId).ToArray();
        var orderLines = await _dbContext.SalesOrderLines
            .Where(x => x.TenantId == tenantId &&
                        x.SalesOrderId == command.SaleId &&
                        selectedLineIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var productPolicyIds = await (
            from line in _dbContext.SalesOrderLines.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on line.ProductId equals product.Id
            where line.TenantId == tenantId &&
                  product.TenantId == tenantId &&
                  selectedLineIds.Contains(line.Id)
            select new { line.Id, product.ReturnPolicyId })
            .ToListAsync(cancellationToken);
        var policyIds = productPolicyIds
            .Where(x => x.ReturnPolicyId.HasValue)
            .Select(x => x.ReturnPolicyId!.Value)
            .Distinct()
            .ToArray();
        var policies = await _dbContext.ReturnPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        policyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var defaultPolicy = await _dbContext.ReturnPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        x.IsDefaultPolicy)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (productPolicyIds.Any(x =>
                x.ReturnPolicyId.HasValue &&
                policies.TryGetValue(x.ReturnPolicyId.Value, out var policy)
                    ? policy.RequiresManagerApproval
                    : defaultPolicy?.RequiresManagerApproval == true))
        {
            return new PosReturnCompleteRepositoryResult(null, "approval_required");
        }

        var payments = await _dbContext.SalesPayments
            .Where(payment => payment.TenantId == tenantId &&
                              payment.SalesOrderId == command.SaleId &&
                              (payment.PaymentStatus == "PAID" ||
                               payment.PaymentStatus == "PARTIALLY_REFUNDED"))
            .OrderBy(payment => payment.PaidAt)
            .ThenBy(payment => payment.Id)
            .ToListAsync(cancellationToken);
        var paymentMethodIds = payments.Select(x => x.PaymentMethodId).Distinct().ToArray();
        var paymentMethods = await _dbContext.PaymentMethods
            .AsNoTracking()
            .Where(method => method.TenantId == tenantId &&
                             paymentMethodIds.Contains(method.Id))
            .ToDictionaryAsync(method => method.Id, cancellationToken);
        var paymentRows = payments
            .Where(payment => paymentMethods.ContainsKey(payment.PaymentMethodId))
            .Select(payment =>
            {
                var method = paymentMethods[payment.PaymentMethodId];
                return new PaymentAllocationRow(
                    payment,
                    method.Id,
                    method.MethodCode,
                    method.MethodName,
                    method.MethodType,
                    method.SupportsRefund);
            })
            .ToList();

        var cashMethod = command.SettlementMethodCode == "CASH_REFUND"
            ? await _dbContext.PaymentMethods
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.MethodCode == "CASH" &&
                            x.IsActiveForPos &&
                            x.SupportsRefund &&
                            x.Status == "ACTIVE")
                .OrderBy(x => x.SortOrder)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        if (command.SettlementMethodCode == "CASH_REFUND" && cashMethod is null)
        {
            return new PosReturnCompleteRepositoryResult(
                null,
                "cash_payment_method_not_found");
        }

        var eligiblePayments = command.SettlementMethodCode == "CARD_REFUND"
            ? paymentRows.Where(x =>
                    x.SupportsRefund &&
                    (x.MethodType == "CARD" || x.MethodCode.Contains("CARD")))
                .ToList()
            : paymentRows;
        var availablePaymentAmount = eligiblePayments.Sum(x =>
            Math.Max(0, x.Payment.PaidAmount - x.Payment.RefundedAmount));
        if (availablePaymentAmount < preview.Calculation.NetCreditAmount)
        {
            return new PosReturnCompleteRepositoryResult(
                null,
                command.SettlementMethodCode == "CARD_REFUND"
                    ? "original_card_payment_required"
                    : "credit_exceeds_refundable");
        }

        var originalStockRows = await (
            from reference in _dbContext.StockMovementReferences.AsNoTracking()
            join movement in _dbContext.StockMovements.AsNoTracking()
                on new { reference.TenantId, Id = reference.StockMovementId }
                equals new { movement.TenantId, movement.Id }
            where reference.TenantId == tenantId &&
                  reference.ReferenceType == "SALES_ORDER" &&
                  reference.ReferenceId == command.SaleId &&
                  reference.ReferenceLineId.HasValue &&
                  selectedLineIds.Contains(reference.ReferenceLineId.Value) &&
                  movement.QuantityChange < 0
            select new OriginalStockRow(
                reference.ReferenceLineId!.Value,
                movement.InventoryBalanceId,
                -movement.QuantityChange))
            .ToListAsync(cancellationToken);
        var priorRestocks = await (
            from returnLine in _dbContext.SalesReturnLines.AsNoTracking()
            join priorReturn in _dbContext.SalesReturns.AsNoTracking()
                on returnLine.SalesReturnId equals priorReturn.Id
            join reference in _dbContext.StockMovementReferences.AsNoTracking()
                on returnLine.Id equals reference.ReferenceLineId
            join movement in _dbContext.StockMovements.AsNoTracking()
                on new { reference.TenantId, Id = reference.StockMovementId }
                equals new { movement.TenantId, movement.Id }
            where returnLine.TenantId == tenantId &&
                  priorReturn.TenantId == tenantId &&
                  priorReturn.SalesOrderId == command.SaleId &&
                  selectedLineIds.Contains(returnLine.SalesOrderLineId) &&
                  reference.ReferenceType == "SALES_RETURN" &&
                  movement.QuantityChange > 0
            group movement by new
            {
                returnLine.SalesOrderLineId,
                movement.InventoryBalanceId
            }
            into restockGroup
            select new PriorRestockRow(
                restockGroup.Key.SalesOrderLineId,
                restockGroup.Key.InventoryBalanceId,
                restockGroup.Sum(x => x.QuantityChange)))
            .ToListAsync(cancellationToken);

        var stockPlan = new List<StockRestorePlan>();
        foreach (var requestedLine in command.Lines)
        {
            var sourceRows = originalStockRows
                .Where(x => x.SaleLineId == requestedLine.SaleLineId)
                .ToList();
            var remaining = requestedLine.ReturnQty;
            foreach (var source in sourceRows)
            {
                var alreadyRestocked = priorRestocks
                    .Where(x => x.SaleLineId == source.SaleLineId &&
                                x.InventoryBalanceId == source.InventoryBalanceId)
                    .Sum(x => x.Quantity);
                var capacity = Math.Max(0, source.SoldQuantity - alreadyRestocked);
                var quantity = Math.Min(remaining, capacity);
                if (quantity <= 0)
                {
                    continue;
                }

                stockPlan.Add(new StockRestorePlan(
                    source.SaleLineId,
                    source.InventoryBalanceId,
                    quantity));
                remaining -= quantity;
                if (remaining <= 0)
                {
                    break;
                }
            }

            if (sourceRows.Count > 0 && remaining > 0)
            {
                return new PosReturnCompleteRepositoryResult(null, "concurrency_conflict");
            }
        }

        var returnId = Guid.NewGuid();
        var refundId = Guid.NewGuid();
        var returnNumber = await GetNextDocumentNumberAsync(
            _dbContext.SalesReturns
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.ReturnNumber),
            "RET-",
            6,
            cancellationToken);
        var refundNumber = await GetNextDocumentNumberAsync(
            _dbContext.SalesRefunds
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.RefundNumber),
            "REF-",
            6,
            cancellationToken);
        var receiptNumber = await GetNextDocumentNumberAsync(
            _dbContext.Receipts
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.ReceiptNumber),
            "RRF-",
            6,
            cancellationToken);

        var totalQuantity = command.Lines.Sum(x => x.ReturnQty);
        var salesReturn = SalesReturn.CreateCompleted(
            returnId,
            tenantId,
            command.SaleId,
            order.CustomerId,
            command.OutletId,
            reason.Id,
            returnNumber,
            totalQuantity,
            preview.Calculation.NetCreditAmount,
            command.Notes,
            command.IdempotencyKey,
            tenantUserId,
            now);
        _dbContext.SalesReturns.Add(salesReturn);
        _dbContext.SalesReturnEvents.Add(SalesReturnEvent.RecordCompleted(
            Guid.NewGuid(),
            tenantId,
            returnId,
            tenantUserId,
            command.Notes,
            now));

        var previewItems = preview.Items.ToDictionary(x => x.SaleLineId);
        var returnLineIds = new Dictionary<Guid, Guid>();
        foreach (var requestLine in command.Lines)
        {
            var orderLine = orderLines.Single(x => x.Id == requestLine.SaleLineId);
            var previewItem = previewItems[requestLine.SaleLineId];
            var soldQty = orderLine.FulfilledQuantity > 0
                ? orderLine.FulfilledQuantity
                : orderLine.Quantity - orderLine.CancelledQuantity;
            var ratio = requestLine.ReturnQty / soldQty;
            var subtotalAfterDiscount = RoundAmount(
                (orderLine.LineSubtotalAmount - orderLine.LineDiscountAmount) * ratio);
            var tax = RoundAmount(orderLine.LineTaxAmount * ratio);
            var returnLineId = Guid.NewGuid();
            returnLineIds.Add(orderLine.Id, returnLineId);
            _dbContext.SalesReturnLines.Add(SalesReturnLine.CreateReceived(
                returnLineId,
                tenantId,
                returnId,
                orderLine.Id,
                reason.Id,
                requestLine.ReturnQty,
                orderLine.UnitPrice,
                RoundAmount(tax / requestLine.ReturnQty),
                subtotalAfterDiscount,
                tax,
                reason.RequiresInspection,
                command.Notes,
                now));
            orderLine.RecordReturn(requestLine.ReturnQty, now);
            _dbContext.SalesRefundLines.Add(SalesRefundLine.Create(
                Guid.NewGuid(),
                tenantId,
                refundId,
                returnLineId,
                previewItem.Name,
                requestLine.ReturnQty,
                previewItem.LineAmount,
                now));
        }

        // Re-load draft lines inside the same serializable transaction before finalizing.
        var draftLines = await _dbContext.ReturnInspectionDraftLines.Where(x =>
            x.TenantId == tenantId && x.ReturnInspectionDraftId == draft.Id).ToListAsync(cancellationToken);
        if (command.Lines.Any(x => draftLines.All(line => line.SaleLineId != x.SaleLineId)))
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_required");
        var activeConditions = await _dbContext.ReturnInspectionConditions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive).ToDictionaryAsync(x => x.ConditionCode, cancellationToken);

        // Pre-validate conditions (including Step 6 approval) before writing final inspections.
        foreach (var requestLine in command.Lines)
        {
            var draftLine = draftLines.Single(x => x.SaleLineId == requestLine.SaleLineId);
            if (!activeConditions.TryGetValue(draftLine.ConditionCodeSnapshot, out var preCondition))
                return new PosReturnCompleteRepositoryResult(null, "inspection_stale");
            if (preCondition.RequiresApproval)
                return new PosReturnCompleteRepositoryResult(null, "approval_required");
        }

        foreach (var requestLine in command.Lines)
        {
            var draftLine = draftLines.Single(x => x.SaleLineId == requestLine.SaleLineId);
            var condition = activeConditions[draftLine.ConditionCodeSnapshot];
            var restock = condition.IsResellable ? requestLine.ReturnQty : 0m;
            var reject = condition.IsResellable ? 0m : requestLine.ReturnQty;
            var inspection = ReturnInspection.CreateCompleted(Guid.NewGuid(), tenantId,
                returnLineIds[requestLine.SaleLineId], tenantUserId, condition.ConditionCode,
                condition.IsResellable ? "RESTOCK" : "REJECT", restock, reject,
                draftLine.InspectionNotes, now);
            _dbContext.ReturnInspections.Add(inspection);
            var stagedMedia = await _dbContext.ReturnInspectionMediaStaging.Where(x =>
                x.TenantId == tenantId && x.InspectionDraftLineId == draftLine.Id && x.Status == "STAGED").ToListAsync(cancellationToken);
            foreach (var staged in stagedMedia)
            {
                _dbContext.ReturnInspectionMedia.Add(ReturnInspectionMedia.Create(Guid.NewGuid(), tenantId,
                    inspection.Id, staged.StorageKey, staged.FileName, staged.ContentType, staged.SizeBytes,
                    staged.UploadedByTenantUserId, now));
                staged.MarkConsumed(now);
            }
        }
        draft.MarkConsumed();

        var refundMode = command.SettlementMethodCode == "CASH_REFUND"
            ? "CASH"
            : "ORIGINAL_PAYMENT";
        _dbContext.SalesRefunds.Add(SalesRefund.CreateCompleted(
            refundId,
            tenantId,
            command.SaleId,
            returnId,
            refundNumber,
            refundMode,
            preview.Currency,
            preview.Calculation.NetCreditAmount,
            reason.ReasonName,
            tenantUserId,
            now));

        var remainingAllocation = preview.Calculation.NetCreditAmount;
        foreach (var paymentRow in eligiblePayments)
        {
            var paymentAvailable = Math.Max(
                0,
                paymentRow.Payment.PaidAmount - paymentRow.Payment.RefundedAmount);
            var allocation = Math.Min(remainingAllocation, paymentAvailable);
            if (allocation <= 0)
            {
                continue;
            }

            var refundPaymentMethodId = cashMethod?.Id ?? paymentRow.PaymentMethodId;
            _dbContext.SalesRefundPaymentAllocations.Add(
                SalesRefundPaymentAllocation.CreateCompleted(
                    Guid.NewGuid(),
                    tenantId,
                    refundId,
                    paymentRow.Payment.Id,
                    refundPaymentMethodId,
                    allocation,
                    command.SettlementMethodCode == "CARD_REFUND"
                        ? $"ORIGINAL-{paymentRow.Payment.PaymentNumber}"
                        : null,
                    now));
            paymentRow.Payment.RecordRefund(allocation, tenantUserId, now);
            remainingAllocation -= allocation;
            if (remainingAllocation <= 0)
            {
                break;
            }
        }

        order.RecordRefund(preview.Calculation.NetCreditAmount, tenantUserId, now);

        var balanceIds = stockPlan.Select(x => x.InventoryBalanceId).Distinct().ToArray();
        var balances = await _dbContext.InventoryBalances
            .Where(x => x.TenantId == tenantId && balanceIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var movementIndex = 0;
        foreach (var stock in stockPlan)
        {
            var balance = balances[stock.InventoryBalanceId];
            var quantityBefore = balance.OnHandQuantity;
            balance.AdjustQuantities(
                stock.Quantity,
                0,
                0,
                reason.RequiresInspection ? stock.Quantity : 0,
                now);
            var movementId = Guid.NewGuid();
            var returnLineId = returnLineIds[stock.SaleLineId];
            _dbContext.StockMovements.Add(StockMovement.Create(
                movementId,
                tenantId,
                $"SM-{returnId:N}-{++movementIndex:D3}",
                balance.Id,
                "RETURN",
                quantityBefore,
                stock.Quantity,
                null,
                null,
                $"return:{returnId:N}:{movementIndex}",
                $"POS return {returnNumber}",
                now,
                tenantUserId,
                now));
            _dbContext.StockMovementReferences.Add(StockMovementReference.Create(
                Guid.NewGuid(),
                tenantId,
                movementId,
                "SALES_RETURN",
                returnId,
                returnLineId,
                now));
        }

        if (command.SettlementMethodCode == "CASH_REFUND")
        {
            _dbContext.TillCashMovements.Add(TillCashMovement.CreateCashOut(
                Guid.NewGuid(),
                tenantId,
                command.TillSessionId,
                preview.Calculation.NetCreditAmount,
                preview.Currency,
                $"Cash refund for {returnNumber}",
                refundNumber,
                tenantUserId,
                now));
        }

        var businessDate = await _dbContext.TillSessions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == command.TillSessionId)
            .Select(x => x.BusinessDate)
            .FirstAsync(cancellationToken);
        var receiptId = Guid.NewGuid();
        var receiptDataJson = JsonSerializer.Serialize(new
        {
            returnId,
            returnNumber,
            refundId,
            refundNumber,
            originalSaleId = command.SaleId,
            originalInvoiceNo = preview.InvoiceNo,
            settlementMethodCode = command.SettlementMethodCode,
            reasonCode = command.ReasonCode,
            items = preview.Items
        });
        _dbContext.Receipts.Add(Receipt.CreateForRefund(
            receiptId,
            tenantId,
            receiptNumber,
            command.SaleId,
            command.OutletId,
            command.TillId,
            command.TillSessionId,
            businessDate,
            tenantUserId,
            preview.Currency,
            preview.Calculation.ItemValue,
            preview.Calculation.DiscountAdjustment,
            preview.Calculation.TaxAdjustment,
            preview.Calculation.NetCreditAmount,
            receiptDataJson,
            now));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return new PosReturnCompleteRepositoryResult(null, "concurrency_conflict");
        }
        catch (DbUpdateException ex) when (IsRefundIdempotencyUniqueViolation(ex))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            var raced = await _dbContext.SalesReturns
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.IdempotencyKey == command.IdempotencyKey)
                .Select(x => new { x.Id, x.SalesOrderId, x.OutletId })
                .FirstOrDefaultAsync(cancellationToken);
            if (raced is null)
            {
                return new PosReturnCompleteRepositoryResult(null, "concurrency_conflict");
            }

            return raced.SalesOrderId == command.SaleId && raced.OutletId == command.OutletId
                ? await GetCompletionAsync(
                    tenantId,
                    command.OutletId,
                    raced.Id,
                    cancellationToken)
                : new PosReturnCompleteRepositoryResult(null, "idempotency_conflict");
        }

        var cashierName = await _dbContext.TenantUsers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == tenantUserId)
            .Select(x => x.DisplayName ?? x.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        var tillName = await _dbContext.Tills
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == command.TillId)
            .Select(x => x.TillName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        var settlementLabel = command.SettlementMethodCode == "CASH_REFUND"
            ? "Cash Refund"
            : "Card Refund";
        var settlementDisplay = command.SettlementMethodCode == "CASH_REFUND"
            ? "Cash"
            : eligiblePayments.First().PaymentMethodName;
        var returnedItems = preview.Items
            .Select(item => new PosReturnCompletionItemDto(
                item.SaleLineId,
                item.Name,
                item.VariantLabel,
                item.ReturnQty,
                item.UnitPrice,
                item.LineAmount,
                item.ImageStorageKey))
            .ToList();

        return new PosReturnCompleteRepositoryResult(
            new PosReturnReceiptDto(
                returnId,
                receiptNumber,
                preview.InvoiceNo,
                preview.Items.Count,
                command.SettlementMethodCode,
                settlementLabel,
                settlementDisplay,
                $"{settlementLabel} completed",
                preview.Currency,
                preview.Calculation.NetCreditAmount,
                0,
                now,
                "COMPLETED",
                preview.CustomerName,
                cashierName,
                tillName,
                "NOT_REQUIRED",
                "NOT_CAPTURED",
                receiptId,
                command.SaleId,
                "REFUND",
                CanPrint: true,
                returnNumber,
                PolicyMessage: null,
                ReturnedItems: returnedItems),
            null);
    }

    private async Task<PosReturnCompleteRepositoryResult> CompleteExchangeAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosReturnCompleteCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(command.ResolutionType, "EXCHANGE", StringComparison.Ordinal))
        {
            return new PosReturnCompleteRepositoryResult(null, "invalid_resolution");
        }

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

        var priorCompletion = await (
            from priorExchange in _dbContext.SalesExchanges.AsNoTracking()
            join priorReturn in _dbContext.SalesReturns.AsNoTracking()
                on priorExchange.SalesReturnId equals priorReturn.Id
            where priorExchange.TenantId == tenantId &&
                  priorExchange.IdempotencyKey == command.IdempotencyKey
            select new { ReturnId = priorReturn.Id, priorReturn.SalesOrderId })
            .FirstOrDefaultAsync(cancellationToken);
        if (priorCompletion is not null)
        {
            return priorCompletion.SalesOrderId == command.SaleId
                ? await GetCompletionAsync(
                    tenantId,
                    command.OutletId,
                    priorCompletion.ReturnId,
                    cancellationToken)
                : new PosReturnCompleteRepositoryResult(null, "idempotency_conflict");
        }

        var draft = await _dbContext.ReturnInspectionDrafts
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.OutletId == command.OutletId &&
                     x.SaleId == command.SaleId,
                cancellationToken);
        if (draft is null)
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_required");
        }
        if (draft.IsExpired(now))
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_expired");
        }
        if (string.Equals(draft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_stale");
        }
        if (!string.Equals(draft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_not_validated");
        }
        if (draft.Version != command.ExpectedVersion)
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_conflict");
        }
        if (!string.Equals(draft.ResolutionType, "EXCHANGE", StringComparison.Ordinal))
        {
            return new PosReturnCompleteRepositoryResult(null, "invalid_resolution");
        }

        var authoritative = await PreviewExchangeAsync(
            tenantId,
            command.OutletId,
            command.SaleId,
            command.ReasonCode,
            command.Lines,
            now,
            cancellationToken);
        if (authoritative.Preview is null)
        {
            return new PosReturnCompleteRepositoryResult(
                null,
                authoritative.ErrorCode ?? "exchange_preview_failed");
        }

        var preview = authoritative.Preview;
        if (!preview.CanProceed)
        {
            return new PosReturnCompleteRepositoryResult(null, "exchange_cannot_proceed");
        }
        if (preview.RequiresApproval || draft.RequiresManagerApproval)
        {
            return new PosReturnCompleteRepositoryResult(null, "approval_required");
        }

        var requiredSettlement = preview.DifferenceDirection switch
        {
            "EVEN_EXCHANGE" => "NO_SETTLEMENT",
            "CUSTOMER_PAYS" => "CASH_PAYMENT",
            "CUSTOMER_RECEIVES" when command.SettlementMethodCode == "CASH_REFUND" => "CASH_REFUND",
            "CUSTOMER_RECEIVES" when command.SettlementMethodCode == "CARD_REFUND" => "CARD_REFUND",
            "CUSTOMER_RECEIVES" => "REFUND_REQUIRED",
            _ => "INVALID",
        };
        if (!string.Equals(command.SettlementMethodCode, requiredSettlement, StringComparison.Ordinal))
        {
            return new PosReturnCompleteRepositoryResult(
                null,
                preview.DifferenceDirection == "CUSTOMER_PAYS" &&
                !string.Equals(command.SettlementMethodCode, "CASH_PAYMENT", StringComparison.Ordinal)
                    ? "exchange_cash_payment_required"
                    : "exchange_settlement_mismatch");
        }

        var order = await _dbContext.SalesOrders.FirstOrDefaultAsync(
            x => x.TenantId == tenantId &&
                 x.Id == command.SaleId &&
                 x.OrderType == "POS_SALE" &&
                 x.Status == "COMPLETED",
            cancellationToken);
        if (order is null)
        {
            return new PosReturnCompleteRepositoryResult(null, "sale_not_found");
        }

        var reason = await _dbContext.ReturnReasons.FirstOrDefaultAsync(
            x => x.TenantId == tenantId &&
                 x.ReasonCode == command.ReasonCode &&
                 x.IsActive &&
                 (x.AppliesTo == "RETURN" || x.AppliesTo == "BOTH"),
            cancellationToken);
        if (reason is null)
        {
            return new PosReturnCompleteRepositoryResult(null, "reason_not_found");
        }

        var selectedLineIds = command.Lines.Select(x => x.SaleLineId).ToArray();
        var orderLines = await _dbContext.SalesOrderLines
            .Where(x => x.TenantId == tenantId &&
                        x.SalesOrderId == command.SaleId &&
                        selectedLineIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (orderLines.Count != selectedLineIds.Length)
        {
            return new PosReturnCompleteRepositoryResult(null, "sale_line_not_found");
        }

        var draftLines = await _dbContext.ReturnInspectionDraftLines
            .Where(x => x.TenantId == tenantId &&
                        x.ReturnInspectionDraftId == draft.Id &&
                        selectedLineIds.Contains(x.SaleLineId))
            .ToDictionaryAsync(x => x.SaleLineId, cancellationToken);
        if (draftLines.Count != selectedLineIds.Length)
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_draft_required");
        }

        var conditionCodes = draftLines.Values
            .Select(x => x.ConditionCodeSnapshot)
            .Distinct()
            .ToArray();
        var conditions = await _dbContext.ReturnInspectionConditions.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.IsActive &&
                        conditionCodes.Contains(x.ConditionCode))
            .ToDictionaryAsync(x => x.ConditionCode, cancellationToken);
        if (conditions.Count != conditionCodes.Length)
        {
            return new PosReturnCompleteRepositoryResult(null, "inspection_stale");
        }
        if (conditions.Values.Any(x => x.RequiresApproval))
        {
            return new PosReturnCompleteRepositoryResult(null, "approval_required");
        }

        var replacementItems = preview.ReplacementItems;
        if (replacementItems.Count == 0 ||
            replacementItems.Select(x => x.ReturnedSaleLineId).Distinct().Count() !=
            replacementItems.Count ||
            replacementItems.Any(x => !selectedLineIds.Contains(x.ReturnedSaleLineId)))
        {
            return new PosReturnCompleteRepositoryResult(null, "replacement_not_found");
        }

        var replacementVariantIds = replacementItems
            .Select(x => x.ReplacementVariantId)
            .Distinct()
            .ToArray();
        var variants = await _dbContext.ProductVariants.AsNoTracking()
            .Where(x => x.TenantId == tenantId && replacementVariantIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (variants.Count != replacementVariantIds.Length)
        {
            return new PosReturnCompleteRepositoryResult(null, "variant_not_found");
        }
        var replacementProductIds = variants.Values.Select(x => x.ProductId).Distinct().ToArray();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == tenantId && replacementProductIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var uomIds = variants.Values.Select(x => x.SalesUomId).Distinct().ToArray();
        var uoms = await _dbContext.UnitOfMeasures.AsNoTracking()
            .Where(x => uomIds.Contains(x.Id) &&
                        (!x.TenantId.HasValue || x.TenantId == tenantId))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (products.Count != replacementProductIds.Length || uoms.Count != uomIds.Length)
        {
            return new PosReturnCompleteRepositoryResult(null, "replacement_not_found");
        }

        var authoritativePricing = await CalculateExchangeReplacementPricingAsync(
            tenantId,
            command.OutletId,
            replacementItems,
            now,
            cancellationToken);
        if (!authoritativePricing.IsSuccess)
        {
            return new PosReturnCompleteRepositoryResult(
                null,
                authoritativePricing.ErrorCode == "insufficient_outlet_stock"
                    ? "insufficient_outlet_stock"
                    : authoritativePricing.ErrorCode ?? "exchange_price_changed");
        }

        if (RoundAmount(authoritativePricing.GrandTotal) != RoundAmount(preview.ReplacementItemValue))
        {
            return new PosReturnCompleteRepositoryResult(null, "exchange_price_changed");
        }

        if (RoundAmount(authoritativePricing.TaxTotal) != RoundAmount(preview.ReplacementTax))
        {
            return new PosReturnCompleteRepositoryResult(null, "exchange_tax_changed");
        }

        if (RoundAmount(authoritativePricing.DiscountTotal) != RoundAmount(preview.ReplacementDiscount))
        {
            return new PosReturnCompleteRepositoryResult(null, "exchange_discount_changed");
        }

        if (!string.IsNullOrWhiteSpace(authoritativePricing.CurrencyCode) &&
            !string.Equals(
                authoritativePricing.CurrencyCode,
                preview.CurrencyCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnCompleteRepositoryResult(null, "exchange_currency_mismatch");
        }

        var pricedByKey = authoritativePricing.Lines.ToDictionary(x => x.LineKey);
        foreach (var item in replacementItems)
        {
            if (!pricedByKey.TryGetValue(item.ReturnedSaleLineId, out var priced) ||
                RoundAmount(priced.UnitPrice) != RoundAmount(item.UnitPrice) ||
                RoundAmount(priced.LineTax) != RoundAmount(item.LineTax) ||
                RoundAmount(priced.LineDiscount) != RoundAmount(item.LineDiscount) ||
                RoundAmount(priced.LineTotal) != RoundAmount(item.LineTotal))
            {
                return new PosReturnCompleteRepositoryResult(null, "exchange_preview_stale");
            }
        }

        var priceListId = authoritativePricing.PriceListId
                          ?? throw new InvalidOperationException("Price list is required.");
        var priceItems = await _dbContext.PriceListItems.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.PriceListId == priceListId &&
                        x.ProductVariantId.HasValue &&
                        replacementVariantIds.Contains(x.ProductVariantId.Value) &&
                        x.Status == "ACTIVE")
            .OrderBy(x => x.MinQuantity)
            .ToListAsync(cancellationToken);

        var replacementBalances = await (
            from balance in _dbContext.InventoryBalances
            join location in _dbContext.InventoryLocations.AsNoTracking()
                on balance.InventoryLocationId equals location.Id
            where balance.TenantId == tenantId &&
                  location.TenantId == tenantId &&
                  location.OutletId == command.OutletId &&
                  location.IsSellableLocation &&
                  location.Status == "ACTIVE" &&
                  balance.ProductVariantId.HasValue &&
                  replacementVariantIds.Contains(balance.ProductVariantId.Value) &&
                  balance.AvailableQuantity > 0
            orderby balance.InventoryLocationId, balance.Id
            select balance)
            .ToListAsync(cancellationToken);
        foreach (var item in replacementItems)
        {
            if (replacementBalances
                    .Where(x => x.ProductVariantId == item.ReplacementVariantId)
                    .Sum(x => x.AvailableQuantity) < item.Quantity)
            {
                return new PosReturnCompleteRepositoryResult(null, "insufficient_outlet_stock");
            }
        }

        var originalStockRows = await (
            from reference in _dbContext.StockMovementReferences.AsNoTracking()
            join movement in _dbContext.StockMovements.AsNoTracking()
                on new { reference.TenantId, Id = reference.StockMovementId }
                equals new { movement.TenantId, movement.Id }
            where reference.TenantId == tenantId &&
                  reference.ReferenceType == "SALES_ORDER" &&
                  reference.ReferenceId == command.SaleId &&
                  reference.ReferenceLineId.HasValue &&
                  selectedLineIds.Contains(reference.ReferenceLineId.Value) &&
                  movement.QuantityChange < 0
            select new OriginalStockRow(
                reference.ReferenceLineId!.Value,
                movement.InventoryBalanceId,
                -movement.QuantityChange))
            .ToListAsync(cancellationToken);
        var priorRestocks = await (
            from returnLine in _dbContext.SalesReturnLines.AsNoTracking()
            join priorReturn in _dbContext.SalesReturns.AsNoTracking()
                on returnLine.SalesReturnId equals priorReturn.Id
            join reference in _dbContext.StockMovementReferences.AsNoTracking()
                on returnLine.Id equals reference.ReferenceLineId
            join movement in _dbContext.StockMovements.AsNoTracking()
                on new { reference.TenantId, Id = reference.StockMovementId }
                equals new { movement.TenantId, movement.Id }
            where returnLine.TenantId == tenantId &&
                  priorReturn.TenantId == tenantId &&
                  priorReturn.SalesOrderId == command.SaleId &&
                  selectedLineIds.Contains(returnLine.SalesOrderLineId) &&
                  reference.ReferenceType == "SALES_RETURN" &&
                  movement.QuantityChange > 0
            group movement by new
            {
                returnLine.SalesOrderLineId,
                movement.InventoryBalanceId
            }
            into restockGroup
            select new PriorRestockRow(
                restockGroup.Key.SalesOrderLineId,
                restockGroup.Key.InventoryBalanceId,
                restockGroup.Sum(x => x.QuantityChange)))
            .ToListAsync(cancellationToken);
        var returnStockPlan = new List<StockRestorePlan>();
        foreach (var requestedLine in command.Lines)
        {
            var remaining = requestedLine.ReturnQty;
            var sources = originalStockRows
                .Where(x => x.SaleLineId == requestedLine.SaleLineId)
                .ToList();
            foreach (var source in sources)
            {
                var alreadyRestocked = priorRestocks
                    .Where(x => x.SaleLineId == source.SaleLineId &&
                                x.InventoryBalanceId == source.InventoryBalanceId)
                    .Sum(x => x.Quantity);
                var quantity = Math.Min(
                    remaining,
                    Math.Max(0, source.SoldQuantity - alreadyRestocked));
                if (quantity <= 0)
                {
                    continue;
                }
                returnStockPlan.Add(new StockRestorePlan(
                    source.SaleLineId,
                    source.InventoryBalanceId,
                    quantity));
                remaining -= quantity;
            }
            if (sources.Count > 0 && remaining > 0)
            {
                return new PosReturnCompleteRepositoryResult(null, "concurrency_conflict");
            }
        }

        var returnId = Guid.NewGuid();
        var replacementOrderId = Guid.NewGuid();
        var exchangeId = Guid.NewGuid();
        var returnNumber = await GetNextDocumentNumberAsync(
            _dbContext.SalesReturns.Where(x => x.TenantId == tenantId).Select(x => x.ReturnNumber),
            "RET-", 6, cancellationToken);
        var replacementOrderNumber = await GetNextDocumentNumberAsync(
            _dbContext.SalesOrders.Where(x => x.TenantId == tenantId).Select(x => x.OrderNumber),
            "EXO-", 6, cancellationToken);
        var exchangeNumber = await GetNextDocumentNumberAsync(
            _dbContext.SalesExchanges.Where(x => x.TenantId == tenantId).Select(x => x.ExchangeNumber),
            "EXC-", 6, cancellationToken);
        var receiptNumber = await GetNextDocumentNumberAsync(
            _dbContext.Receipts.Where(x => x.TenantId == tenantId).Select(x => x.ReceiptNumber),
            "XRC-", 6, cancellationToken);

        var returnValue = preview.ReturnItemValue;
        var replacementValue = authoritativePricing.GrandTotal;
        var replacementSubtotal = authoritativePricing.Subtotal;
        var replacementDiscount = authoritativePricing.DiscountTotal;
        var replacementTax = authoritativePricing.TaxTotal;
        var difference = preview.DifferenceAmount;
        var salesReturn = SalesReturn.CreateCompletedExchange(
            returnId,
            tenantId,
            command.SaleId,
            order.CustomerId,
            command.OutletId,
            reason.Id,
            returnNumber,
            command.Lines.Sum(x => x.ReturnQty),
            returnValue,
            command.Notes,
            tenantUserId,
            now);
        _dbContext.SalesReturns.Add(salesReturn);
        _dbContext.SalesReturnEvents.Add(SalesReturnEvent.RecordCompleted(
            Guid.NewGuid(), tenantId, returnId, tenantUserId, command.Notes, now));

        var creditItems = (await PreviewCreditAsync(
                tenantId,
                command.SaleId,
                command.ReasonCode,
                command.Lines,
                now,
                cancellationToken))
            .Preview!.Items.ToDictionary(x => x.SaleLineId);
        var returnLineIds = new Dictionary<Guid, Guid>();
        foreach (var requestedLine in command.Lines)
        {
            var sourceLine = orderLines[requestedLine.SaleLineId];
            var creditItem = creditItems[requestedLine.SaleLineId];
            var soldQuantity = sourceLine.FulfilledQuantity > 0
                ? sourceLine.FulfilledQuantity
                : sourceLine.Quantity - sourceLine.CancelledQuantity;
            var ratio = requestedLine.ReturnQty / soldQuantity;
            var subtotal = RoundAmount(
                (sourceLine.LineSubtotalAmount - sourceLine.LineDiscountAmount) * ratio);
            var tax = RoundAmount(sourceLine.LineTaxAmount * ratio);
            var returnLineId = Guid.NewGuid();
            returnLineIds.Add(sourceLine.Id, returnLineId);
            _dbContext.SalesReturnLines.Add(SalesReturnLine.CreateReceived(
                returnLineId,
                tenantId,
                returnId,
                sourceLine.Id,
                reason.Id,
                requestedLine.ReturnQty,
                sourceLine.UnitPrice,
                RoundAmount(tax / requestedLine.ReturnQty),
                subtotal,
                tax,
                reason.RequiresInspection,
                command.Notes,
                now));
            sourceLine.RecordReturn(requestedLine.ReturnQty, now);

            var draftLine = draftLines[sourceLine.Id];
            var condition = conditions[draftLine.ConditionCodeSnapshot];
            var restock = condition.IsResellable ? requestedLine.ReturnQty : 0m;
            var reject = condition.IsResellable ? 0m : requestedLine.ReturnQty;
            var inspection = ReturnInspection.CreateCompleted(
                Guid.NewGuid(),
                tenantId,
                returnLineId,
                tenantUserId,
                condition.ConditionCode,
                condition.IsResellable ? "RESTOCK" : "REJECT",
                restock,
                reject,
                draftLine.InspectionNotes,
                now);
            _dbContext.ReturnInspections.Add(inspection);
            var stagedMedia = await _dbContext.ReturnInspectionMediaStaging
                .Where(x => x.TenantId == tenantId &&
                            x.InspectionDraftLineId == draftLine.Id &&
                            x.Status == "STAGED")
                .ToListAsync(cancellationToken);
            foreach (var staged in stagedMedia)
            {
                _dbContext.ReturnInspectionMedia.Add(ReturnInspectionMedia.Create(
                    Guid.NewGuid(),
                    tenantId,
                    inspection.Id,
                    staged.StorageKey,
                    staged.FileName,
                    staged.ContentType,
                    staged.SizeBytes,
                    staged.UploadedByTenantUserId,
                    now));
                staged.MarkConsumed(now);
            }
        }

        var replacementOrder = SalesOrder.CreateCompletedExchangeOrder(
            replacementOrderId,
            tenantId,
            replacementOrderNumber,
            order.SalesChannelId,
            order.CustomerId,
            order.CustomerNameSnapshot,
            command.TillId,
            command.TillSessionId,
            priceListId,
            preview.CurrencyCode,
            replacementSubtotal,
            replacementDiscount,
            replacementTax,
            replacementValue,
            replacementValue,
            tenantUserId,
            now);
        _dbContext.SalesOrders.Add(replacementOrder);

        var replacementOrderLineIds = new Dictionary<Guid, Guid>();
        var replacementMovementIndex = 0;
        for (var index = 0; index < replacementItems.Count; index++)
        {
            var item = replacementItems[index];
            var variant = variants[item.ReplacementVariantId];
            var product = products[variant.ProductId];
            var uom = uoms[variant.SalesUomId];
            var priced = pricedByKey[item.ReturnedSaleLineId];
            var priceItem = priceItems.FirstOrDefault(x => x.Id == priced.PriceListItemId)
                            ?? priceItems
                                .Where(x => x.ProductVariantId == variant.Id &&
                                            x.MinQuantity <= item.Quantity)
                                .OrderByDescending(x => x.MinQuantity)
                                .FirstOrDefault();
            if (priceItem is null)
            {
                return new PosReturnCompleteRepositoryResult(null, "exchange_price_changed");
            }

            var replacementLineId = Guid.NewGuid();
            replacementOrderLineIds.Add(item.ReturnedSaleLineId, replacementLineId);
            _dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
                replacementLineId,
                tenantId,
                replacementOrderId,
                index + 1,
                product.Id,
                variant.Id,
                variant.SalesUomId,
                priceItem.Id,
                variant.Sku,
                null,
                product.ProductName,
                variant.VariantName,
                null,
                null,
                null,
                null,
                uom.UomCode,
                uom.UomName,
                product.ProductType,
                product.ProductStructure,
                item.Quantity,
                priced.UnitPrice,
                priced.LineSubtotal,
                priced.LineDiscount,
                priced.LineTax,
                false,
                now));

            var remaining = item.Quantity;
            foreach (var balance in replacementBalances
                         .Where(x => x.ProductVariantId == item.ReplacementVariantId))
            {
                var quantity = Math.Min(remaining, balance.AvailableQuantity);
                if (quantity <= 0)
                {
                    continue;
                }
                var quantityBefore = balance.OnHandQuantity;
                balance.AdjustQuantities(-quantity, 0m, 0m, 0m, now);
                var movementId = Guid.NewGuid();
                _dbContext.StockMovements.Add(StockMovement.Create(
                    movementId,
                    tenantId,
                    $"SM-{replacementOrderId:N}-{++replacementMovementIndex:D3}",
                    balance.Id,
                    "SALE",
                    quantityBefore,
                    -quantity,
                    null,
                    null,
                    $"exchange:{exchangeId:N}:sale:{replacementMovementIndex}",
                    $"POS exchange {exchangeNumber}",
                    now,
                    tenantUserId,
                    now));
                _dbContext.StockMovementReferences.Add(StockMovementReference.Create(
                    Guid.NewGuid(),
                    tenantId,
                    movementId,
                    "SALES_ORDER",
                    replacementOrderId,
                    replacementLineId,
                    now));
                remaining -= quantity;
                if (remaining <= 0)
                {
                    break;
                }
            }
            if (remaining > 0)
            {
                return new PosReturnCompleteRepositoryResult(null, "insufficient_outlet_stock");
            }
        }

        var returnBalanceIds = returnStockPlan.Select(x => x.InventoryBalanceId).Distinct().ToArray();
        var returnBalances = await _dbContext.InventoryBalances
            .Where(x => x.TenantId == tenantId && returnBalanceIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var returnMovementIndex = 0;
        foreach (var stock in returnStockPlan)
        {
            if (!returnBalances.TryGetValue(stock.InventoryBalanceId, out var balance))
            {
                return new PosReturnCompleteRepositoryResult(null, "concurrency_conflict");
            }
            var condition = conditions[draftLines[stock.SaleLineId].ConditionCodeSnapshot];
            var quantityBefore = balance.OnHandQuantity;
            balance.AdjustQuantities(
                stock.Quantity,
                0m,
                condition.IsResellable ? 0m : stock.Quantity,
                0m,
                now);
            var movementId = Guid.NewGuid();
            _dbContext.StockMovements.Add(StockMovement.Create(
                movementId,
                tenantId,
                $"SM-{returnId:N}-{++returnMovementIndex:D3}",
                balance.Id,
                "RETURN",
                quantityBefore,
                stock.Quantity,
                null,
                null,
                $"exchange:{exchangeId:N}:return:{returnMovementIndex}",
                $"POS exchange return {returnNumber}",
                now,
                tenantUserId,
                now));
            _dbContext.StockMovementReferences.Add(StockMovementReference.Create(
                Guid.NewGuid(),
                tenantId,
                movementId,
                "SALES_RETURN",
                returnId,
                returnLineIds[stock.SaleLineId],
                now));
        }

        var exchange = SalesExchange.CreateCompleted(
            exchangeId,
            tenantId,
            returnId,
            replacementOrderId,
            exchangeNumber,
            preview.DifferenceDirection,
            difference,
            preview.DifferenceDirection == "CUSTOMER_PAYS" ? difference : 0m,
            preview.DifferenceDirection == "CUSTOMER_RECEIVES" ? difference : 0m,
            command.Notes,
            command.IdempotencyKey,
            tenantUserId,
            now);
        _dbContext.SalesExchanges.Add(exchange);
        foreach (var item in replacementItems)
        {
            _dbContext.SalesExchangeLines.Add(SalesExchangeLine.Create(
                Guid.NewGuid(),
                tenantId,
                exchangeId,
                returnLineIds[item.ReturnedSaleLineId],
                item.ReplacementProductId,
                item.ReplacementVariantId,
                replacementOrderLineIds[item.ReturnedSaleLineId],
                item.Quantity,
                creditItems[item.ReturnedSaleLineId].LineAmount,
                item.LineTotal,
                now));
        }
        _dbContext.SalesExchangeEvents.Add(SalesExchangeEvent.RecordCompleted(
            Guid.NewGuid(), tenantId, exchangeId, tenantUserId, command.Notes, now));

        if (preview.DifferenceDirection == "CUSTOMER_PAYS")
        {
            var cashMethod = await _dbContext.PaymentMethods.AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.MethodCode == "CASH" &&
                            x.IsActiveForPos &&
                            x.Status == "ACTIVE")
                .OrderBy(x => x.SortOrder)
                .FirstOrDefaultAsync(cancellationToken);
            if (cashMethod is null)
            {
                return new PosReturnCompleteRepositoryResult(null, "cash_payment_method_not_found");
            }
            var paymentId = Guid.NewGuid();
            var paymentNumber = await GetNextDocumentNumberAsync(
                _dbContext.SalesPayments
                    .Where(x => x.TenantId == tenantId)
                    .Select(x => x.PaymentNumber),
                "PAY-", 6, cancellationToken);
            var paymentPersistence = PosCompletedPaymentPersistence.CreateCash(
                paymentId,
                tenantId,
                replacementOrderId,
                paymentNumber,
                cashMethod.Id,
                command.TillId,
                command.TillSessionId,
                preview.CurrencyCode,
                difference,
                difference,
                difference,
                0m,
                $"exchange-payment-{exchangeId:N}",
                command.IdempotencyKey,
                tenantUserId,
                now);
            _dbContext.SalesPayments.Add(paymentPersistence.Payment);
            _dbContext.SalesPaymentTransactions.Add(paymentPersistence.Transaction);
            _dbContext.SalesPaymentEvents.Add(paymentPersistence.Event);
            _dbContext.TillCashMovements.Add(TillCashMovement.CreateCashIn(
                Guid.NewGuid(),
                tenantId,
                command.TillSessionId,
                difference,
                preview.CurrencyCode,
                $"Cash payment for exchange {exchangeNumber}",
                paymentNumber,
                tenantUserId,
                now));
        }
        else if (preview.DifferenceDirection == "CUSTOMER_RECEIVES")
        {
            var originalPayments = await _dbContext.SalesPayments
                .Where(x => x.TenantId == tenantId &&
                            x.SalesOrderId == command.SaleId &&
                            (x.PaymentStatus == "PAID" ||
                             x.PaymentStatus == "PARTIALLY_REFUNDED"))
                .OrderBy(x => x.PaidAt)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
            var methodIds = originalPayments.Select(x => x.PaymentMethodId).Distinct().ToArray();
            var methods = await _dbContext.PaymentMethods.AsNoTracking()
                .Where(x => x.TenantId == tenantId && methodIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            var cashMethod = command.SettlementMethodCode == "CASH_REFUND"
                ? await _dbContext.PaymentMethods.AsNoTracking()
                    .Where(x => x.TenantId == tenantId &&
                                x.MethodCode == "CASH" &&
                                x.IsActiveForPos &&
                                x.SupportsRefund &&
                                x.Status == "ACTIVE")
                    .OrderBy(x => x.SortOrder)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;
            if (command.SettlementMethodCode == "CASH_REFUND" && cashMethod is null)
            {
                return new PosReturnCompleteRepositoryResult(null, "cash_payment_method_not_found");
            }
            var eligiblePayments = originalPayments
                .Where(x => methods.TryGetValue(x.PaymentMethodId, out var method) &&
                            (command.SettlementMethodCode == "CASH_REFUND" ||
                             (method.SupportsRefund &&
                              (method.MethodType == "CARD" ||
                               method.MethodCode.Contains("CARD")))))
                .ToList();
            if (eligiblePayments.Sum(x => Math.Max(0m, x.PaidAmount - x.RefundedAmount)) <
                difference)
            {
                return new PosReturnCompleteRepositoryResult(
                    null,
                    command.SettlementMethodCode == "CARD_REFUND"
                        ? "original_card_payment_required"
                        : "credit_exceeds_refundable");
            }

            var refundId = Guid.NewGuid();
            var refundNumber = await GetNextDocumentNumberAsync(
                _dbContext.SalesRefunds
                    .Where(x => x.TenantId == tenantId)
                    .Select(x => x.RefundNumber),
                "REF-", 6, cancellationToken);
            _dbContext.SalesRefunds.Add(SalesRefund.CreateCompleted(
                refundId,
                tenantId,
                command.SaleId,
                returnId,
                refundNumber,
                command.SettlementMethodCode == "CASH_REFUND" ? "CASH" : "ORIGINAL_PAYMENT",
                preview.CurrencyCode,
                difference,
                reason.ReasonName,
                tenantUserId,
                now));
            var refundRatio = difference / returnValue;
            foreach (var item in creditItems.Values)
            {
                _dbContext.SalesRefundLines.Add(SalesRefundLine.Create(
                    Guid.NewGuid(),
                    tenantId,
                    refundId,
                    returnLineIds[item.SaleLineId],
                    item.Name,
                    item.ReturnQty,
                    RoundAmount(item.LineAmount * refundRatio),
                    now));
            }
            var remainingRefund = difference;
            foreach (var payment in eligiblePayments)
            {
                var allocation = Math.Min(
                    remainingRefund,
                    Math.Max(0m, payment.PaidAmount - payment.RefundedAmount));
                if (allocation <= 0)
                {
                    continue;
                }
                _dbContext.SalesRefundPaymentAllocations.Add(
                    SalesRefundPaymentAllocation.CreateCompleted(
                        Guid.NewGuid(),
                        tenantId,
                        refundId,
                        payment.Id,
                        cashMethod?.Id ?? payment.PaymentMethodId,
                        allocation,
                        command.SettlementMethodCode == "CARD_REFUND"
                            ? $"ORIGINAL-{payment.PaymentNumber}"
                            : null,
                        now));
                payment.RecordRefund(allocation, tenantUserId, now);
                remainingRefund -= allocation;
                if (remainingRefund <= 0)
                {
                    break;
                }
            }
            order.RecordRefund(difference, tenantUserId, now);
            if (command.SettlementMethodCode == "CASH_REFUND")
            {
                _dbContext.TillCashMovements.Add(TillCashMovement.CreateCashOut(
                    Guid.NewGuid(),
                    tenantId,
                    command.TillSessionId,
                    difference,
                    preview.CurrencyCode,
                    $"Cash refund for exchange {exchangeNumber}",
                    refundNumber,
                    tenantUserId,
                    now));
            }
        }

        var businessDate = await _dbContext.TillSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == command.TillSessionId)
            .Select(x => x.BusinessDate)
            .FirstAsync(cancellationToken);
        var receiptId = Guid.NewGuid();
        _dbContext.Receipts.Add(Receipt.CreateForExchange(
            receiptId,
            tenantId,
            receiptNumber,
            command.SaleId,
            command.OutletId,
            command.TillId,
            command.TillSessionId,
            businessDate,
            tenantUserId,
            preview.CurrencyCode,
            replacementValue,
            0m,
            0m,
            replacementValue,
            preview.DifferenceDirection == "CUSTOMER_PAYS" ? difference : 0m,
            0m,
            JsonSerializer.Serialize(new
            {
                exchangeId,
                exchangeNumber,
                returnId,
                returnNumber,
                originalSaleId = command.SaleId,
                replacementOrderId,
                replacementOrderNumber,
                returnValue,
                replacementValue,
                difference,
                differenceDirection = preview.DifferenceDirection,
                settlementMethodCode = command.SettlementMethodCode,
                returnedItems = creditItems.Values,
                replacementItems
            }),
            now));
        draft.MarkConsumed();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return new PosReturnCompleteRepositoryResult(null, "concurrency_conflict");
        }
        catch (DbUpdateException)
        {
            return new PosReturnCompleteRepositoryResult(null, "duplicate_completion");
        }

        return await GetCompletionAsync(
            tenantId,
            command.OutletId,
            returnId,
            cancellationToken);
    }

    public async Task<PosReturnCompleteRepositoryResult> GetCompletionAsync(
        Guid tenantId,
        Guid outletId,
        Guid returnId,
        CancellationToken cancellationToken)
    {
        var salesReturn = await _dbContext.SalesReturns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == returnId, cancellationToken);
        if (salesReturn is null)
        {
            return new PosReturnCompleteRepositoryResult(null, "completion_not_found");
        }

        if (salesReturn.OutletId.HasValue && salesReturn.OutletId.Value != outletId)
        {
            return new PosReturnCompleteRepositoryResult(null, "completion_not_found");
        }

        if (!string.Equals(salesReturn.ReturnStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnCompleteRepositoryResult(null, "completion_not_ready");
        }

        var exchange = await _dbContext.SalesExchanges.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.SalesReturnId == returnId,
                cancellationToken);

        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SaleId == salesReturn.SalesOrderId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var draftResolution = draft?.ResolutionType?.Trim().ToUpperInvariant();
        if (string.Equals(draftResolution, "EXCHANGE", StringComparison.OrdinalIgnoreCase) &&
            exchange is null)
        {
            return new PosReturnCompleteRepositoryResult(null, "exchange_incomplete");
        }

        var resolution = exchange is not null || salesReturn.TotalExchangeAmount > 0
            ? "EXCHANGE"
            : "REFUND";

        if (string.Equals(resolution, "REFUND", StringComparison.OrdinalIgnoreCase))
        {
            var refund = await _dbContext.SalesRefunds.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId && x.SalesReturnId == returnId,
                    cancellationToken);
            if (refund is null ||
                !string.Equals(refund.RefundStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                return new PosReturnCompleteRepositoryResult(null, "completion_not_ready");
            }
        }

        var receiptTypes = resolution == "EXCHANGE"
            ? new[] { "EXCHANGE", "REFUND" }
            : new[] { "REFUND" };
        var receipt = await _dbContext.Receipts.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.SalesOrderId == salesReturn.SalesOrderId &&
                        receiptTypes.Contains(x.ReceiptType))
            .OrderByDescending(x => x.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (receipt is null)
        {
            return new PosReturnCompleteRepositoryResult(null, "receipt_not_found");
        }

        var returnLines = await _dbContext.SalesReturnLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesReturnId == returnId)
            .ToListAsync(cancellationToken);
        var orderLineIds = returnLines.Select(x => x.SalesOrderLineId).ToArray();
        var orderLines = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && orderLineIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var reasonIds = returnLines
            .Where(x => x.ReturnReasonId.HasValue)
            .Select(x => x.ReturnReasonId!.Value)
            .Distinct()
            .ToArray();
        var reasons = reasonIds.Length == 0
            ? new Dictionary<Guid, (string Code, string Name)>()
            : await _dbContext.ReturnReasons.AsNoTracking()
                .Where(x => x.TenantId == tenantId && reasonIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => (Code: x.ReasonCode, Name: x.ReasonName),
                    cancellationToken);

        var returnLineIds = returnLines.Select(x => x.Id).ToArray();
        var inspectionRows = returnLineIds.Length == 0
            ? []
            : await _dbContext.ReturnInspections.AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            returnLineIds.Contains(x.SalesReturnLineId))
                .Select(x => new
                {
                    x.SalesReturnLineId,
                    x.ConditionCode,
                    x.InspectedAt,
                    x.CreatedAt
                })
                .ToListAsync(cancellationToken);
        var inspections = inspectionRows
            .GroupBy(x => x.SalesReturnLineId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(i => i.InspectedAt ?? i.CreatedAt)
                    .Select(i => i.ConditionCode)
                    .FirstOrDefault());

        var conditionCodes = inspections.Values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();
        var conditionNameRows = conditionCodes.Length == 0
            ? []
            : await _dbContext.ReturnInspectionConditions.AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            conditionCodes.Contains(x.ConditionCode))
                .Select(x => new { x.ConditionCode, x.DisplayName })
                .ToListAsync(cancellationToken);
        var conditionNames = conditionNameRows
            .GroupBy(x => x.ConditionCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().DisplayName,
                StringComparer.OrdinalIgnoreCase);

        var productIds = orderLines.Values.Select(x => x.ProductId).Distinct().ToArray();
        var imageByProduct = productIds.Length == 0
            ? new Dictionary<Guid, string?>()
            : (await _dbContext.ProductImages.AsNoTracking()
                .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
                .OrderByDescending(x => x.IsPrimaryImage)
                .ThenBy(x => x.Id)
                .Select(x => new { x.ProductId, x.ImageUrl, x.ImageStorageKey })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => ResolveImageValue(g.First().ImageUrl, g.First().ImageStorageKey));

        var returnedItems = returnLines.Select(line =>
        {
            orderLines.TryGetValue(line.SalesOrderLineId, out var orderLine);
            var qty = line.QuantityReceived ?? line.QuantityRequested;
            var soldQty = orderLine is null
                ? 0m
                : orderLine.FulfilledQuantity > 0
                    ? orderLine.FulfilledQuantity
                    : Math.Max(0, orderLine.Quantity - orderLine.CancelledQuantity);
            var ratio = orderLine is null || soldQty <= 0 ? 0m : qty / soldQty;
            var discount = orderLine is null
                ? 0m
                : RoundAmount(orderLine.LineDiscountAmount * ratio);
            var grossSubtotal = orderLine is null
                ? line.LineSubtotalAmount + discount
                : RoundAmount(orderLine.LineSubtotalAmount * ratio);
            var tax = line.LineTaxAmount;
            var total = line.LineSubtotalAmount + line.LineTaxAmount;
            string? reasonCode = null;
            string? reasonDisplay = null;
            if (line.ReturnReasonId.HasValue &&
                reasons.TryGetValue(line.ReturnReasonId.Value, out var reason))
            {
                reasonCode = reason.Code;
                reasonDisplay = reason.Name;
            }

            inspections.TryGetValue(line.Id, out var conditionCode);
            string? conditionDisplay = null;
            if (!string.IsNullOrWhiteSpace(conditionCode) &&
                conditionNames.TryGetValue(conditionCode, out var displayName))
            {
                conditionDisplay = displayName;
            }

            string? imageKey = null;
            if (orderLine is not null)
            {
                imageByProduct.TryGetValue(orderLine.ProductId, out imageKey);
            }

            return new PosReturnCompletionItemDto(
                line.SalesOrderLineId,
                orderLine?.ProductNameSnapshot ?? "Returned item",
                orderLine?.VariantNameSnapshot ?? string.Empty,
                qty,
                line.UnitPriceSnapshot,
                total,
                imageKey,
                IsReplacement: false,
                SalesReturnLineId: line.Id,
                ProductId: orderLine?.ProductId,
                VariantId: orderLine?.ProductVariantId,
                Sku: orderLine?.SkuSnapshot,
                Subtotal: grossSubtotal,
                Discount: discount,
                Tax: tax,
                Total: total,
                ReasonCode: reasonCode,
                ReasonDisplay: reasonDisplay,
                ConditionCode: conditionCode,
                ConditionDisplay: conditionDisplay,
                Disposition: line.DispositionStatus,
                Currency: receipt.CurrencyCode);
        }).ToList();

        IReadOnlyList<PosReturnCompletionItemDto> replacementItems =
            Array.Empty<PosReturnCompletionItemDto>();
        string? replacementOrderNumber = null;
        decimal replacementSubtotal = 0;
        decimal replacementDiscount = 0;
        decimal replacementTax = 0;
        decimal replacementTotal = 0;
        if (exchange?.ReplacementSalesOrderId is Guid replacementOrderId)
        {
            var replacementOrder = await _dbContext.SalesOrders.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId && x.Id == replacementOrderId,
                    cancellationToken);
            replacementOrderNumber = replacementOrder?.OrderNumber;
            var replacementLines = await _dbContext.SalesOrderLines.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.SalesOrderId == replacementOrderId)
                .OrderBy(x => x.LineNumber)
                .ToListAsync(cancellationToken);
            var replacementProductIds = replacementLines.Select(x => x.ProductId).Distinct().ToArray();
            var replacementImages = replacementProductIds.Length == 0
                ? new Dictionary<Guid, string?>()
                : (await _dbContext.ProductImages.AsNoTracking()
                    .Where(x => x.TenantId == tenantId &&
                                replacementProductIds.Contains(x.ProductId))
                    .OrderByDescending(x => x.IsPrimaryImage)
                    .ThenBy(x => x.Id)
                    .Select(x => new { x.ProductId, x.ImageUrl, x.ImageStorageKey })
                    .ToListAsync(cancellationToken))
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => ResolveImageValue(g.First().ImageUrl, g.First().ImageStorageKey));

            replacementItems = replacementLines
                .Select(line =>
                {
                    var lineTotal = line.LineSubtotalAmount - line.LineDiscountAmount + line.LineTaxAmount;
                    replacementSubtotal += line.LineSubtotalAmount;
                    replacementDiscount += line.LineDiscountAmount;
                    replacementTax += line.LineTaxAmount;
                    replacementTotal += lineTotal;
                    replacementImages.TryGetValue(line.ProductId, out var imageKey);
                    return new PosReturnCompletionItemDto(
                        line.Id,
                        line.ProductNameSnapshot,
                        line.VariantNameSnapshot ?? string.Empty,
                        line.Quantity,
                        line.UnitPrice,
                        lineTotal,
                        imageKey,
                        IsReplacement: true,
                        ReplacementOrderLineId: line.Id,
                        ProductId: line.ProductId,
                        VariantId: line.ProductVariantId,
                        Sku: line.SkuSnapshot,
                        Subtotal: line.LineSubtotalAmount,
                        Discount: line.LineDiscountAmount,
                        Tax: line.LineTaxAmount,
                        Total: lineTotal,
                        Currency: receipt.CurrencyCode);
                })
                .ToList();
        }

        var refundRecord = await _dbContext.SalesRefunds.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.SalesReturnId == returnId,
                cancellationToken);

        var processedByUserId = salesReturn.CreatedByTenantUserId;
        var cashierName = processedByUserId.HasValue
            ? await _dbContext.TenantUsers.AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.Id == processedByUserId.Value)
                .Select(x => x.DisplayName ?? x.FullName)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty
            : string.Empty;

        string settlementMethodCode;
        string settlementMethodLabel;
        string settlementDisplay;
        string settlementResult;
        decimal refundAmount;
        decimal customerCreditAmount;
        string? differenceDirection = null;
        decimal? differenceAmount = null;
        decimal? returnItemValue = null;
        decimal? replacementItemValue = null;
        decimal? amountPaidByCustomer = null;
        decimal? amountRefundedToCustomer = null;
        decimal? amountDueFromCustomer = null;
        decimal? amountDueToCustomer = null;

        if (resolution == "EXCHANGE" && exchange is not null)
        {
            settlementMethodCode = string.Equals(
                    exchange.ExchangeMode,
                    "STORE_CREDIT",
                    StringComparison.OrdinalIgnoreCase)
                ? "EVEN_EXCHANGE"
                : exchange.ExchangeMode;
            settlementMethodLabel = settlementMethodCode switch
            {
                "CUSTOMER_PAYS" => "Customer Pays",
                "CUSTOMER_RECEIVES" => "Customer Receives",
                "NO_SETTLEMENT" => "No Settlement",
                _ => "Even Exchange",
            };
            settlementDisplay = settlementMethodLabel;
            settlementResult = $"Exchange {exchange.ExchangeNumber} completed";
            refundAmount = exchange.RefundBackAmount;
            customerCreditAmount = salesReturn.TotalExchangeAmount;
            differenceDirection = settlementMethodCode;
            differenceAmount = exchange.PriceDifferenceAmount;
            returnItemValue = salesReturn.TotalExchangeAmount;
            replacementItemValue = replacementTotal;
            amountDueFromCustomer = settlementMethodCode == "CUSTOMER_PAYS"
                ? exchange.PriceDifferenceAmount
                : 0m;
            amountDueToCustomer = settlementMethodCode == "CUSTOMER_RECEIVES"
                ? exchange.PriceDifferenceAmount
                : 0m;
            amountPaidByCustomer = amountDueFromCustomer;
            amountRefundedToCustomer = amountDueToCustomer;
        }
        else
        {
            settlementMethodCode = refundRecord?.RefundMode == "CASH"
                ? "CASH_REFUND"
                : "CARD_REFUND";
            if (string.Equals(settlementMethodCode, "STORE_CREDIT", StringComparison.OrdinalIgnoreCase))
            {
                settlementMethodCode = "CASH_REFUND";
            }

            settlementMethodLabel = settlementMethodCode == "CASH_REFUND"
                ? "Cash Refund"
                : "Card Refund";
            settlementDisplay = settlementMethodLabel;
            settlementResult = $"{settlementMethodLabel} completed";
            refundAmount = refundRecord?.RefundedAmount ?? salesReturn.TotalRefundAmount;
            customerCreditAmount = 0;
            amountRefundedToCustomer = refundAmount;
            amountDueToCustomer = refundAmount;
            amountPaidByCustomer = 0m;
            amountDueFromCustomer = 0m;
        }

        var originalSale = await _dbContext.SalesOrders.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == salesReturn.SalesOrderId)
            .Select(x => new
            {
                x.OrderNumber,
                x.CustomerId,
                x.CustomerNameSnapshot
            })
            .FirstOrDefaultAsync(cancellationToken);

        var originalInvoice = await _dbContext.Receipts.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.SalesOrderId == salesReturn.SalesOrderId &&
                        x.ReceiptType == "SALE")
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => x.ReceiptNumber)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var customerDisplayName = !string.IsNullOrWhiteSpace(originalSale?.CustomerNameSnapshot)
            ? originalSale!.CustomerNameSnapshot!.Trim()
            : string.Empty;
        if (string.IsNullOrWhiteSpace(customerDisplayName) && salesReturn.CustomerId.HasValue)
        {
            customerDisplayName = await _dbContext.Customers.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == salesReturn.CustomerId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        }

        var tillName = await _dbContext.Tills.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == receipt.TillId)
            .Select(x => x.TillName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var outletName = await _dbContext.Outlets.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == receipt.OutletId)
            .Select(x => x.OutletName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var deviceInfo = await _dbContext.TillSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == receipt.TillSessionId)
            .Select(x => new { x.OpenedFromPosDeviceId })
            .FirstOrDefaultAsync(cancellationToken);
        Guid? deviceId = deviceInfo?.OpenedFromPosDeviceId;
        string? deviceName = null;
        if (deviceId.HasValue)
        {
            deviceName = await _dbContext.PosDevices.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == deviceId.Value)
                .Select(x => x.DeviceName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        string? cardBrand = null;
        string? maskedCard = null;
        string? providerTransactionReference = null;
        string? paymentRefundStatus = null;
        if (refundRecord is not null)
        {
            paymentRefundStatus = refundRecord.RefundStatus;
            var allocation = await _dbContext.SalesRefundPaymentAllocations.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.SalesRefundId == refundRecord.Id)
                .OrderByDescending(x => x.AllocatedAmount)
                .Select(x => new { x.OriginalSalesPaymentId, x.ExternalReference })
                .FirstOrDefaultAsync(cancellationToken);
            if (allocation is not null)
            {
                var safeAllocationRef = allocation.ExternalReference?.Trim();
                if (!string.IsNullOrWhiteSpace(safeAllocationRef) &&
                    !LooksLikeSensitivePaymentSecret(safeAllocationRef))
                {
                    providerTransactionReference = safeAllocationRef;
                }

                var originalPayment = await _dbContext.SalesPayments.AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.Id == allocation.OriginalSalesPaymentId)
                    .Select(x => new { x.Id, x.ExternalReference, x.PaymentNumber, x.PaymentMethodId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (originalPayment is not null)
                {
                    if (string.IsNullOrWhiteSpace(providerTransactionReference))
                    {
                        var paymentRef = !string.IsNullOrWhiteSpace(originalPayment.ExternalReference)
                            ? originalPayment.ExternalReference.Trim()
                            : originalPayment.PaymentNumber;
                        if (!string.IsNullOrWhiteSpace(paymentRef) &&
                            !LooksLikeSensitivePaymentSecret(paymentRef))
                        {
                            providerTransactionReference = paymentRef;
                        }
                    }

                    if (string.Equals(settlementMethodCode, "CARD_REFUND", StringComparison.OrdinalIgnoreCase))
                    {
                        var sanitizedJson = await _dbContext.SalesPaymentTransactions.AsNoTracking()
                            .Where(t => t.TenantId == tenantId &&
                                        t.SalesPaymentId == originalPayment.Id &&
                                        t.TransactionStatus == "SUCCEEDED")
                            .OrderBy(t => t.ProcessedAt)
                            .Select(t => t.ProviderResponseJson)
                            .FirstOrDefaultAsync(cancellationToken);
                        (cardBrand, _) = SafePaymentDisplay.TryParseSanitizedCardMetadata(sanitizedJson);
                        maskedCard = SafePaymentDisplay.FormatMaskedReference(
                            SafePaymentDisplay.ResolveLast4(
                                sanitizedJson,
                                originalPayment.ExternalReference));
                    }
                }
            }
        }

        if (string.Equals(settlementMethodCode, "CASH_REFUND", StringComparison.OrdinalIgnoreCase))
        {
            cardBrand = null;
            maskedCard = null;
        }

        if (!string.IsNullOrWhiteSpace(maskedCard))
        {
            settlementDisplay = string.IsNullOrWhiteSpace(cardBrand)
                ? $"{settlementMethodLabel} {maskedCard}"
                : $"{cardBrand} {maskedCard}";
        }

        var printCount = await _dbContext.ReceiptPrintLogs.AsNoTracking()
            .CountAsync(
                x => x.TenantId == tenantId && x.ReceiptId == receipt.Id,
                cancellationToken);

        var returnSubtotal = returnedItems.Sum(x => x.Subtotal ?? 0m);
        var returnDiscount = returnedItems.Sum(x => x.Discount ?? 0m);
        var returnTax = returnedItems.Sum(x => x.Tax ?? 0m);
        var returnTotal = returnedItems.Sum(x => x.Total ?? x.LineAmount);

        return new PosReturnCompleteRepositoryResult(
            new PosReturnReceiptDto(
                salesReturn.Id,
                receipt.ReceiptNumber,
                originalInvoice,
                returnedItems.Count,
                settlementMethodCode,
                settlementMethodLabel,
                settlementDisplay,
                settlementResult,
                receipt.CurrencyCode,
                refundAmount,
                customerCreditAmount,
                salesReturn.CompletedAt ?? salesReturn.CreatedAt,
                "COMPLETED",
                customerDisplayName,
                cashierName,
                tillName,
                "NOT_REQUIRED",
                "NOT_CAPTURED",
                receipt.Id,
                salesReturn.SalesOrderId,
                resolution,
                CanPrint: true,
                salesReturn.ReturnNumber,
                exchange?.ExchangeNumber,
                exchange?.Id,
                replacementOrderNumber,
                PolicyMessage: null,
                returnedItems,
                replacementItems,
                returnItemValue,
                replacementItemValue,
                differenceAmount,
                differenceDirection,
                receipt.OutletId,
                outletName,
                receipt.TillId,
                deviceId,
                deviceName,
                originalSale?.CustomerId ?? salesReturn.CustomerId,
                customerDisplayName,
                processedByUserId,
                cashierName,
                receipt.ReceiptType,
                originalSale?.OrderNumber,
                cardBrand,
                maskedCard,
                providerTransactionReference,
                paymentRefundStatus,
                amountPaidByCustomer,
                amountRefundedToCustomer,
                amountDueFromCustomer,
                amountDueToCustomer,
                returnSubtotal,
                returnDiscount,
                returnTax,
                returnTotal,
                replacementItems.Count == 0 ? null : replacementSubtotal,
                replacementItems.Count == 0 ? null : replacementDiscount,
                replacementItems.Count == 0 ? null : replacementTax,
                replacementItems.Count == 0 ? null : replacementTotal,
                printCount,
                printCount > 0),
            null);
    }

    public async Task<PosReturnCreditPreviewRepositoryResult> PreviewCreditAsync(
        Guid tenantId,
        Guid saleId,
        string reasonCode,
        IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var header = await (
            from order in _dbContext.SalesOrders.AsNoTracking()
            join receipt in _dbContext.Receipts.AsNoTracking()
                on order.Id equals receipt.SalesOrderId
            join customer in _dbContext.Customers.AsNoTracking()
                on order.CustomerId equals customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            where order.TenantId == tenantId &&
                  order.Id == saleId &&
                  receipt.TenantId == tenantId &&
                  order.OrderType == "POS_SALE" &&
                  order.Status == "COMPLETED" &&
                  (order.PaymentStatus == "PAID" ||
                   order.PaymentStatus == "PARTIALLY_REFUNDED") &&
                  receipt.ReceiptType == "SALE" &&
                  receipt.ReceiptStatus == "ISSUED"
            select new CreditPreviewHeader(
                order.Id,
                receipt.ReceiptNumber,
                order.CustomerId,
                order.CustomerNameSnapshot ??
                    (customer != null ? customer.Name : string.Empty) ?? string.Empty,
                customer != null ? customer.CustomerCode : string.Empty,
                order.CompletedAt ?? receipt.IssuedAt,
                receipt.IssuedAt,
                receipt.CurrencyCode,
                receipt.TotalAmount,
                order.RefundedAmount,
                (from payment in _dbContext.SalesPayments
                 join method in _dbContext.PaymentMethods
                     on payment.PaymentMethodId equals method.Id
                 where payment.TenantId == tenantId &&
                       method.TenantId == tenantId &&
                       payment.SalesOrderId == order.Id &&
                       (payment.PaymentStatus == "PAID" ||
                        payment.PaymentStatus == "PARTIALLY_REFUNDED")
                 orderby payment.PaidAt
                 select method.MethodName).FirstOrDefault() ?? string.Empty))
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return new PosReturnCreditPreviewRepositoryResult(null, "sale_not_found");
        }

        var reason = await _dbContext.ReturnReasons
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.ReasonCode == reasonCode &&
                        x.IsActive &&
                        (x.AppliesTo == "RETURN" || x.AppliesTo == "BOTH"))
            .Select(x => new { x.ReasonCode, x.ReasonName })
            .FirstOrDefaultAsync(cancellationToken);
        if (reason is null)
        {
            return new PosReturnCreditPreviewRepositoryResult(null, "reason_not_found");
        }

        var selectedLineIds = lines.Select(x => x.SaleLineId).ToArray();
        var lineRows = await (
            from line in _dbContext.SalesOrderLines.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on line.ProductId equals product.Id
            where line.TenantId == tenantId &&
                  line.SalesOrderId == saleId &&
                  product.TenantId == tenantId &&
                  selectedLineIds.Contains(line.Id)
            orderby line.LineNumber
            select new CreditPreviewLineRow(
                line.Id,
                line.ProductId,
                line.ProductNameSnapshot,
                line.VariantNameSnapshot,
                line.SkuSnapshot,
                line.Quantity,
                line.FulfilledQuantity,
                line.CancelledQuantity,
                line.ReturnedQuantity,
                line.UnitPrice,
                line.LineSubtotalAmount,
                line.LineDiscountAmount,
                line.LineTaxAmount,
                product.ReturnPolicyId))
            .ToListAsync(cancellationToken);
        if (lineRows.Count != selectedLineIds.Length)
        {
            return new PosReturnCreditPreviewRepositoryResult(null, "sale_line_not_found");
        }

        var policyIds = lineRows
            .Where(x => x.ReturnPolicyId.HasValue)
            .Select(x => x.ReturnPolicyId!.Value)
            .Distinct()
            .ToArray();
        var policies = await _dbContext.ReturnPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        policyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var defaultPolicy = await _dbContext.ReturnPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        x.IsDefaultPolicy)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var returnedQuantities = await (
            from returnLine in _dbContext.SalesReturnLines.AsNoTracking()
            join salesReturn in _dbContext.SalesReturns.AsNoTracking()
                on returnLine.SalesReturnId equals salesReturn.Id
            where returnLine.TenantId == tenantId &&
                  salesReturn.TenantId == tenantId &&
                  salesReturn.SalesOrderId == saleId &&
                  selectedLineIds.Contains(returnLine.SalesOrderLineId) &&
                  salesReturn.ReturnStatus != "CANCELLED" &&
                  salesReturn.ReturnStatus != "REJECTED"
            group returnLine by returnLine.SalesOrderLineId into returnGroup
            select new
            {
                SaleLineId = returnGroup.Key,
                Quantity = returnGroup.Sum(x =>
                    x.QuantityReceived ?? x.QuantityRequested)
            })
            .ToDictionaryAsync(x => x.SaleLineId, x => x.Quantity, cancellationToken);

        var productIds = lineRows.Select(x => x.ProductId).Distinct().ToArray();
        var imageRows = await _dbContext.ProductImages
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        productIds.Contains(x.ProductId) &&
                        x.Status == "ACTIVE")
            .OrderByDescending(x => x.IsPrimaryImage)
            .ThenBy(x => x.SortOrder)
            .Select(x => new { x.ProductId, x.ImageUrl, x.ImageStorageKey })
            .ToListAsync(cancellationToken);
        var images = imageRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group => ResolveImageValue(
                    group.First().ImageUrl,
                    group.First().ImageStorageKey));

        var requestedQuantities = lines.ToDictionary(x => x.SaleLineId, x => x.ReturnQty);
        var items = new List<PosReturnCreditPreviewItemDto>(lineRows.Count);
        decimal itemValue = 0;
        decimal discountAdjustment = 0;
        decimal taxAdjustment = 0;

        foreach (var row in lineRows)
        {
            var soldQty = row.FulfilledQuantity > 0
                ? row.FulfilledQuantity
                : Math.Max(0, row.Quantity - row.CancelledQuantity);
            returnedQuantities.TryGetValue(row.SaleLineId, out var recordedReturnQty);
            var returnedQty = Math.Max(row.ReturnedQuantity, recordedReturnQty);
            var availableQty = Math.Max(0, soldQty - returnedQty);
            var requestedQty = requestedQuantities[row.SaleLineId];
            var policy = row.ReturnPolicyId.HasValue &&
                         policies.TryGetValue(row.ReturnPolicyId.Value, out var specificPolicy)
                ? specificPolicy
                : defaultPolicy;

            if (soldQty <= 0 || policy is null ||
                now > header.IssuedAt.AddDays(policy.ReturnWindowDays))
            {
                return new PosReturnCreditPreviewRepositoryResult(null, "line_not_returnable");
            }

            if (requestedQty > availableQty)
            {
                return new PosReturnCreditPreviewRepositoryResult(
                    null,
                    "quantity_exceeds_available");
            }

            var ratio = requestedQty / soldQty;
            var lineItemValue = RoundAmount(row.LineSubtotal * ratio);
            var lineDiscount = RoundAmount(row.LineDiscount * ratio);
            var lineTax = RoundAmount(row.LineTax * ratio);
            var lineAmount = RoundAmount(lineItemValue - lineDiscount + lineTax);
            itemValue += lineItemValue;
            discountAdjustment += lineDiscount;
            taxAdjustment += lineTax;
            images.TryGetValue(row.ProductId, out var imageStorageKey);

            items.Add(new PosReturnCreditPreviewItemDto(
                row.SaleLineId,
                row.ProductName,
                row.Sku ?? string.Empty,
                row.VariantName ?? string.Empty,
                imageStorageKey,
                requestedQty,
                row.UnitPrice,
                lineAmount));
        }

        itemValue = RoundAmount(itemValue);
        discountAdjustment = RoundAmount(discountAdjustment);
        taxAdjustment = RoundAmount(taxAdjustment);
        var netCreditAmount = RoundAmount(itemValue - discountAdjustment + taxAdjustment);
        var remainingRefundable = Math.Max(0, header.SaleTotal - header.RefundedAmount);
        if (netCreditAmount <= 0 || netCreditAmount > remainingRefundable)
        {
            return new PosReturnCreditPreviewRepositoryResult(
                null,
                "credit_exceeds_refundable");
        }

        var saleItemCount = await _dbContext.SalesOrderLines
            .AsNoTracking()
            .CountAsync(
                x => x.TenantId == tenantId && x.SalesOrderId == saleId,
                cancellationToken);
        var preview = new PosReturnCreditPreviewDto(
            header.SaleId,
            header.InvoiceNo,
            header.CustomerId,
            header.CustomerName,
            header.CustomerDisplayId,
            header.SaleDate,
            header.PaymentMethod,
            string.Empty,
            header.Currency,
            header.SaleTotal,
            saleItemCount,
            reason.ReasonCode,
            reason.ReasonName,
            items,
            new PosReturnCreditCalculationDto(
                itemValue,
                "Discount adjustment",
                discountAdjustment,
                "Tax refund",
                taxAdjustment,
                netCreditAmount),
            $"PREVIEW-{header.InvoiceNo}",
            0,
            null,
            items.Count);

        return new PosReturnCreditPreviewRepositoryResult(preview, null);
    }

    public async Task<PosReturnSaleEligibilityDto?> GetSaleEligibilityAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var header = await (
            from order in _dbContext.SalesOrders.AsNoTracking()
            join receipt in _dbContext.Receipts.AsNoTracking()
                on order.Id equals receipt.SalesOrderId
            join customer in _dbContext.Customers.AsNoTracking()
                on order.CustomerId equals customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            where order.TenantId == tenantId &&
                  order.Id == saleId &&
                  receipt.TenantId == tenantId &&
                  receipt.OutletId == outletId &&
                  order.OrderType == "POS_SALE" &&
                  order.Status == "COMPLETED" &&
                  (order.PaymentStatus == "PAID" ||
                   order.PaymentStatus == "PARTIALLY_REFUNDED") &&
                  receipt.ReceiptType == "SALE" &&
                  receipt.ReceiptStatus == "ISSUED"
            select new EligibilityHeader(
                order.Id,
                receipt.ReceiptNumber,
                order.CustomerId,
                order.CustomerNameSnapshot ??
                    (customer != null ? customer.Name : string.Empty) ?? string.Empty,
                order.CompletedAt ?? receipt.IssuedAt,
                receipt.CurrencyCode,
                receipt.IssuedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        var paymentDisplays = await LoadPaymentDisplaysAsync(
            tenantId,
            [saleId],
            cancellationToken);
        var paymentDisplay = paymentDisplays.GetValueOrDefault(
            saleId,
            PaymentDisplay.Empty);

        var lineRows = await (
            from line in _dbContext.SalesOrderLines.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on line.ProductId equals product.Id
            where line.TenantId == tenantId &&
                  line.SalesOrderId == saleId &&
                  product.TenantId == tenantId &&
                  line.LineStatus != "CANCELLED"
            orderby line.LineNumber
            select new EligibilityLineRow(
                line.Id,
                line.ProductId,
                line.ProductVariantId,
                line.ProductNameSnapshot,
                line.VariantNameSnapshot,
                line.SkuSnapshot,
                line.Quantity,
                line.FulfilledQuantity,
                line.CancelledQuantity,
                line.ReturnedQuantity,
                line.UnitPrice,
                line.LineTotalAmount,
                product.ReturnPolicyId))
            .ToListAsync(cancellationToken);

        var productIds = lineRows.Select(x => x.ProductId).Distinct().ToArray();
        var variantIds = lineRows
            .Where(x => x.VariantId.HasValue)
            .Select(x => x.VariantId!.Value)
            .Distinct()
            .ToArray();
        var imageRows = await _dbContext.ProductImages
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        productIds.Contains(x.ProductId) &&
                        x.Status == "ACTIVE")
            .OrderByDescending(x => x.IsPrimaryImage)
            .ThenBy(x => x.SortOrder)
            .Select(x => new
            {
                x.ProductId,
                x.ProductVariantId,
                x.ImageUrl,
                x.ImageStorageKey
            })
            .ToListAsync(cancellationToken);

        var barcodeRows = await _dbContext.ProductBarcodes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        ((x.ProductVariantId.HasValue &&
                          variantIds.Contains(x.ProductVariantId.Value)) ||
                         (!x.ProductVariantId.HasValue &&
                          productIds.Contains(x.ProductId))))
            .OrderByDescending(x => x.IsPrimaryBarcode)
            .ThenBy(x => x.Barcode)
            .Select(x => new
            {
                x.ProductId,
                x.ProductVariantId,
                x.Barcode
            })
            .ToListAsync(cancellationToken);

        var barcodeByVariant = barcodeRows
            .Where(x => x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(group => group.Key, group => group.First().Barcode);
        var barcodeByProduct = barcodeRows
            .Where(x => !x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductId)
            .ToDictionary(group => group.Key, group => group.First().Barcode);

        var imageByVariant = imageRows
            .Where(x => x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(
                group => group.Key,
                group => ResolveImageValue(group.First().ImageUrl, group.First().ImageStorageKey));
        var imageByProduct = imageRows
            .Where(x => !x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group => ResolveImageValue(group.First().ImageUrl, group.First().ImageStorageKey));
        var imageByProductAny = imageRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group => ResolveImageValue(group.First().ImageUrl, group.First().ImageStorageKey));

        var policyIds = lineRows
            .Where(x => x.ReturnPolicyId.HasValue)
            .Select(x => x.ReturnPolicyId!.Value)
            .Distinct()
            .ToArray();
        var policies = await _dbContext.ReturnPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        policyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var defaultPolicy = await _dbContext.ReturnPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        x.IsDefaultPolicy)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var returnedQuantities = await (
            from returnLine in _dbContext.SalesReturnLines.AsNoTracking()
            join salesReturn in _dbContext.SalesReturns.AsNoTracking()
                on returnLine.SalesReturnId equals salesReturn.Id
            where returnLine.TenantId == tenantId &&
                  salesReturn.TenantId == tenantId &&
                  salesReturn.SalesOrderId == saleId &&
                  salesReturn.ReturnStatus == "COMPLETED"
            group returnLine by returnLine.SalesOrderLineId into returnGroup
            select new
            {
                SaleLineId = returnGroup.Key,
                Quantity = returnGroup.Sum(x =>
                    x.QuantityReceived ?? x.QuantityRequested)
            })
            .ToDictionaryAsync(x => x.SaleLineId, x => x.Quantity, cancellationToken);

        var items = new List<PosReturnSaleLineEligibilityDto>(lineRows.Count);
        foreach (var row in lineRows)
        {
            var soldQty = row.FulfilledQuantity > 0
                ? row.FulfilledQuantity
                : Math.Max(0, row.Quantity - row.CancelledQuantity);
            returnedQuantities.TryGetValue(row.SaleLineId, out var recordedReturnQty);
            var returnedQty = Math.Max(row.ReturnedQuantity, recordedReturnQty);
            var availableQty = Math.Max(0, soldQty - returnedQty);
            var policy = row.ReturnPolicyId.HasValue &&
                         policies.TryGetValue(row.ReturnPolicyId.Value, out var specificPolicy)
                ? specificPolicy
                : defaultPolicy;

            var withinWindow = policy is not null &&
                               now <= header.IssuedAt.AddDays(policy.ReturnWindowDays);
            var isReturnable = policy is not null && withinWindow && availableQty > 0;
            var (status, reason) = GetEligibilityStatus(policy is not null, withinWindow, availableQty);

            string? imageStorageKey = null;
            if (row.VariantId.HasValue &&
                imageByVariant.TryGetValue(row.VariantId.Value, out var variantImage))
            {
                imageStorageKey = variantImage;
            }
            else if (imageByProduct.TryGetValue(row.ProductId, out var productImage))
            {
                imageStorageKey = productImage;
            }
            else
            {
                imageByProductAny.TryGetValue(row.ProductId, out imageStorageKey);
            }

            string? barcode = null;
            if (row.VariantId.HasValue &&
                barcodeByVariant.TryGetValue(row.VariantId.Value, out var variantBarcode))
            {
                barcode = variantBarcode;
            }
            else
            {
                barcodeByProduct.TryGetValue(row.ProductId, out barcode);
            }

            var name = string.IsNullOrWhiteSpace(row.VariantName)
                ? row.ProductName
                : $"{row.ProductName} - {row.VariantName}";

            items.Add(new PosReturnSaleLineEligibilityDto(
                row.SaleLineId,
                row.VariantId,
                name,
                row.Sku ?? string.Empty,
                imageStorageKey,
                soldQty,
                returnedQty,
                availableQty,
                row.UnitPrice,
                row.LineTotal,
                isReturnable,
                status,
                reason,
                Barcode: barcode));
        }

        var policyCount = items.Count(x => x.EligibilityStatus != "NO_POLICY");
        var withinWindowCount = items.Count(x =>
            x.EligibilityStatus is not "NO_POLICY" and not "WINDOW_EXPIRED");
        var quantityAvailableCount = items.Count(x => x.AvailableReturnQty > 0);
        var checks = new List<PosReturnPolicyCheckDto>
        {
            new("Original receipt", "Issued receipt verified", true),
            new("Sale status", "Completed and paid", true),
            new(
                "Return policy",
                $"{policyCount} of {items.Count} item(s) have an active policy",
                items.Count > 0 && policyCount == items.Count),
            new(
                "Return window",
                $"{withinWindowCount} of {items.Count} item(s) are within the return window",
                items.Count > 0 && withinWindowCount == items.Count),
            new(
                "Returnable quantity",
                $"{quantityAvailableCount} of {items.Count} item(s) have quantity available",
                quantityAvailableCount > 0)
        };

        return new PosReturnSaleEligibilityDto(
            header.SaleId,
            header.InvoiceNo,
            header.CustomerId,
            header.CustomerName,
            header.SaleDate,
            paymentDisplay.Method,
            paymentDisplay.MaskedReference ?? string.Empty,
            header.Currency,
            items,
            checks);
    }

    public async Task<PosReturnSaleEligibilityCheckResult> CheckSelectedSaleEligibilityAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var baseEligibility = await GetSaleEligibilityAsync(
            tenantId,
            outletId,
            saleId,
            now,
            cancellationToken);
        if (baseEligibility is null)
        {
            return new PosReturnSaleEligibilityCheckResult(null, "sale_not_found");
        }

        var requestedLines = lines
            .GroupBy(line => line.SaleLineId)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.ReturnQty));
        if (requestedLines.Count == 0)
        {
            return new PosReturnSaleEligibilityCheckResult(null, "lines_required");
        }

        var itemById = baseEligibility.Items.ToDictionary(item => item.SaleLineId);
        foreach (var saleLineId in requestedLines.Keys)
        {
            if (!itemById.ContainsKey(saleLineId))
            {
                return new PosReturnSaleEligibilityCheckResult(null, "invalid_sale_line");
            }
        }

        foreach (var (saleLineId, requestedQty) in requestedLines)
        {
            var item = itemById[saleLineId];
            if (!item.IsReturnable || item.AvailableReturnQty <= 0)
            {
                return new PosReturnSaleEligibilityCheckResult(null, "line_not_returnable");
            }

            if (requestedQty > item.AvailableReturnQty)
            {
                return new PosReturnSaleEligibilityCheckResult(null, "quantity_exceeds_available");
            }
        }

        var selectedLineIds = requestedLines.Keys.ToArray();
        var policyContexts = await (
            from line in _dbContext.SalesOrderLines.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on line.ProductId equals product.Id
            where line.TenantId == tenantId &&
                  line.SalesOrderId == saleId &&
                  selectedLineIds.Contains(line.Id) &&
                  product.TenantId == tenantId
            select new
            {
                line.Id,
                product.ReturnPolicyId
            })
            .ToListAsync(cancellationToken);

        var policyIds = policyContexts
            .Where(x => x.ReturnPolicyId.HasValue)
            .Select(x => x.ReturnPolicyId!.Value)
            .Distinct()
            .ToArray();
        var policies = await _dbContext.ReturnPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        policyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var defaultPolicy = await _dbContext.ReturnPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == "ACTIVE" &&
                        x.IsDefaultPolicy)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var selectedItems = new List<PosReturnSaleLineEligibilityDto>(requestedLines.Count);
        var resolvedPolicies = new List<ReturnPolicy?>(requestedLines.Count);
        foreach (var (saleLineId, requestedQty) in requestedLines)
        {
            var item = itemById[saleLineId];
            var eligibleQty = item.IsReturnable
                ? Math.Min(requestedQty, item.AvailableReturnQty)
                : 0m;
            selectedItems.Add(item with
            {
                RequestedReturnQty = requestedQty,
                EligibleReturnQty = eligibleQty
            });

            var policyContext = policyContexts.FirstOrDefault(x => x.Id == saleLineId);
            ReturnPolicy? policy = null;
            if (policyContext is not null)
            {
                policy = policyContext.ReturnPolicyId.HasValue &&
                         policies.TryGetValue(policyContext.ReturnPolicyId.Value, out var specificPolicy)
                    ? specificPolicy
                    : defaultPolicy;
            }
            else
            {
                policy = defaultPolicy;
            }

            resolvedPolicies.Add(policy);
        }

        // Hierarchy: product.ReturnPolicyId (ACTIVE) → tenant default ACTIVE policy.
        // Categories have no return-policy attribute in the current schema.
        var requiresReceipt = resolvedPolicies.Any(policy => policy?.RequiresReceipt == true);
        var requiresManagerApproval = resolvedPolicies.Any(policy =>
            policy?.RequiresManagerApproval == true);
        // ReturnPolicy has no RequiresInspection column; Step 4 preliminary flag stays false.
        // Step 5 reason.RequiresInspection may raise the final requirement.
        const bool requiresInspection = false;

        var receiptCheck = await EvaluateOriginalReceiptCheckAsync(
            tenantId,
            outletId,
            saleId,
            requiresReceipt,
            cancellationToken);
        var paymentCheck = await EvaluateOriginalPaymentCheckAsync(
            tenantId,
            saleId,
            baseEligibility.Currency,
            cancellationToken);

        var policyDescriptions = resolvedPolicies
            .Select(policy => policy?.Description?.Trim())
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Select(description => description!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var windowDays = resolvedPolicies
            .Where(policy => policy is not null)
            .Select(policy => policy!.ReturnWindowDays)
            .DefaultIfEmpty(defaultPolicy?.ReturnWindowDays ?? 0)
            .Max();

        var checks = BuildSelectedLinePolicyChecks(
            selectedItems,
            resolvedPolicies,
            receiptCheck,
            paymentCheck,
            requiresInspection,
            requiresManagerApproval,
            windowDays);
        var summary = BuildEligibilitySummary(
            selectedItems,
            checks,
            requiresInspection,
            requiresManagerApproval,
            policyDescriptions,
            windowDays);

        return new PosReturnSaleEligibilityCheckResult(
            baseEligibility with
            {
                Items = selectedItems,
                PolicyChecks = checks,
                OverallStatus = summary.OverallStatus,
                CanContinue = summary.CanContinue,
                EligibleItemCount = summary.EligibleItemCount,
                SelectedItemCount = summary.SelectedItemCount,
                OverallMessage = summary.OverallMessage,
                PolicyNote = summary.PolicyNote,
                RequiresInspection = requiresInspection,
                RequiresManagerApproval = requiresManagerApproval
            },
            null);
    }

    public async Task<IReadOnlyList<PosReturnReasonOptionDto>> GetActiveReturnReasonsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ReturnReasons
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.IsActive &&
                        (x.AppliesTo == "RETURN" || x.AppliesTo == "BOTH"))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ReasonName)
            .Select(x => new PosReturnReasonOptionDto(
                x.Id,
                x.ReasonCode,
                x.ReasonName,
                x.Description,
                x.SortOrder,
                x.AppliesTo == "RETURN" || x.AppliesTo == "BOTH",
                x.AppliesTo == "EXCHANGE" || x.AppliesTo == "BOTH",
                x.RequiresNote,
                x.RequiresInspection,
                x.RequiresManagerApproval))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PosReturnInspectionConditionDto>> GetActiveInspectionConditionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ReturnInspectionConditions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new PosReturnInspectionConditionDto(
                x.Id,
                x.ConditionCode,
                x.DisplayName,
                x.Description,
                x.StatusCategory,
                x.SortOrder,
                x.IsResellable,
                x.RefundImpact,
                x.RequiresNotes,
                x.RequiresPhoto,
                x.RequiresApproval))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SaleLineBelongsToSaleAsync(
        Guid tenantId,
        Guid saleId,
        Guid saleLineId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SalesOrderLines
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.SalesOrderId == saleId &&
                     x.Id == saleLineId,
                cancellationToken);
    }

    public async Task<bool> SaleBelongsToOutletAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        return await (
            from order in _dbContext.SalesOrders.AsNoTracking()
            join receipt in _dbContext.Receipts.AsNoTracking()
                on order.Id equals receipt.SalesOrderId
            where order.TenantId == tenantId &&
                  order.Id == saleId &&
                  receipt.TenantId == tenantId &&
                  receipt.OutletId == outletId
            select order.Id).AnyAsync(cancellationToken);
    }

    public async Task<PosReturnInspectionMediaStagingResult> SaveInspectionMediaStagingAsync(
        Guid tenantId,
        Guid outletId,
        Guid tenantUserId,
        Guid saleId,
        Guid saleLineId,
        Guid mediaId,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var saleOk = await SaleBelongsToOutletAsync(tenantId, outletId, saleId, cancellationToken);
        if (!saleOk)
        {
            return new PosReturnInspectionMediaStagingResult(null, "pos_returns.sale_not_found");
        }

        var belongs = await SaleLineBelongsToSaleAsync(tenantId, saleId, saleLineId, cancellationToken);
        if (!belongs)
        {
            return new PosReturnInspectionMediaStagingResult(null, "pos_returns.sale_line_not_found");
        }

        var draft = await _dbContext.ReturnInspectionDrafts
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.OutletId == outletId &&
                     x.SaleId == saleId,
                cancellationToken);

        if (draft is not null)
        {
            if (string.Equals(draft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
            {
                return new PosReturnInspectionMediaStagingResult(null, "pos_returns.inspection_draft_consumed");
            }

            if (draft.IsExpired(now))
            {
                return new PosReturnInspectionMediaStagingResult(null, "pos_returns.inspection_draft_expired");
            }
        }

        // Serialize concurrent uploads for the same sale line within this transaction.
        var existingCount = await _dbContext.ReturnInspectionMediaStaging
            .Where(x => x.TenantId == tenantId &&
                        x.SaleId == saleId &&
                        x.SaleLineId == saleLineId &&
                        x.Status == "STAGED")
            .CountAsync(cancellationToken);
        if (existingCount >= 5)
        {
            return new PosReturnInspectionMediaStagingResult(
                null,
                "pos_returns.inspection_media_limit_reached");
        }

        var expiresAt = draft?.ExpiresAt ?? now.AddHours(ReturnInspectionDraft.DefaultLifetimeHours);
        var entity = ReturnInspectionMediaStaging.Create(
            mediaId,
            tenantId,
            saleId,
            saleLineId,
            storageKey,
            fileName,
            contentType,
            sizeBytes,
            tenantUserId,
            now,
            outletId,
            expiresAt);

        if (draft is not null)
        {
            var draftLine = await _dbContext.ReturnInspectionDraftLines
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId &&
                         x.ReturnInspectionDraftId == draft.Id &&
                         x.SaleLineId == saleLineId,
                    cancellationToken);
            if (draftLine is not null)
            {
                entity.AttachToDraftLine(draft.Id, draftLine.Id, draft.ExpiresAt);
            }
        }

        _dbContext.ReturnInspectionMediaStaging.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new PosReturnInspectionMediaStagingResult(
            new PosReturnInspectionMediaStagingRecord(
                entity.Id,
                entity.SaleId,
                entity.SaleLineId,
                entity.StorageKey,
                entity.FileName,
                entity.ContentType,
                entity.SizeBytes,
                entity.Status,
                entity.InspectionDraftId,
                entity.InspectionDraftLineId,
                entity.ExpiresAt,
                entity.OutletId),
            null);
    }

    public async Task<PosReturnInspectionDraftRecord?> GetInspectionDraftBySaleAsync(
        Guid tenantId, Guid outletId, Guid saleId, CancellationToken cancellationToken)
    {
        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);
        return draft is null ? null : await MapDraftAsync(draft, cancellationToken);
    }

    public async Task<PosReturnInspectionDraftSaveResult> SaveInspectionDraftAsync(
        Guid tenantId, Guid outletId, Guid saleId, Guid userId,
        IReadOnlyList<PosReturnInspectionDraftLineDto> lines, DateTimeOffset now,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

        var saleOk = await SaleBelongsToOutletAsync(tenantId, outletId, saleId, cancellationToken);
        if (!saleOk)
        {
            return new PosReturnInspectionDraftSaveResult(null, "pos_returns.sale_not_found");
        }

        var draft = await _dbContext.ReturnInspectionDrafts
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);

        if (draft is null)
        {
            draft = ReturnInspectionDraft.Create(Guid.NewGuid(), tenantId, outletId, saleId, userId, now);
            _dbContext.ReturnInspectionDrafts.Add(draft);
        }
        else
        {
            if (string.Equals(draft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
            {
                return new PosReturnInspectionDraftSaveResult(null, "pos_returns.inspection_draft_consumed");
            }

            if (draft.IsExpired(now))
            {
                return new PosReturnInspectionDraftSaveResult(null, "pos_returns.inspection_draft_expired");
            }

            if (expectedVersion.HasValue && expectedVersion.Value != draft.Version)
            {
                return new PosReturnInspectionDraftSaveResult(null, "pos_returns.inspection_draft_conflict");
            }

            draft.MarkDraft(now);
            await ClearExchangeReplacementLinesAsync(draft.Id, tenantId, cancellationToken);
        }

        var conditions = await _dbContext.ReturnInspectionConditions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive).ToDictionaryAsync(x => x.ConditionCode, cancellationToken);
        foreach (var input in lines)
        {
            var lineBelongs = await SaleLineBelongsToSaleAsync(tenantId, saleId, input.SaleLineId, cancellationToken);
            if (!lineBelongs)
            {
                return new PosReturnInspectionDraftSaveResult(null, "pos_returns.invalid_sale_line_id");
            }

            var existing = await _dbContext.ReturnInspectionDraftLines.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.ReturnInspectionDraftId == draft.Id && x.SaleLineId == input.SaleLineId, cancellationToken);
            conditions.TryGetValue(input.ConditionCode.Trim().ToUpperInvariant(), out var condition);
            var line = existing ?? ReturnInspectionDraftLine.Create(Guid.NewGuid(), tenantId, draft.Id, input.SaleLineId,
                condition?.Id, input.ConditionCode, input.Notes, userId, now);
            if (existing is null) _dbContext.ReturnInspectionDraftLines.Add(line);
            else line.Upsert(condition?.Id, input.ConditionCode, input.Notes, userId, now);
            var mediaIds = input.MediaIds ?? Array.Empty<Guid>();
            var media = await _dbContext.ReturnInspectionMediaStaging.Where(x =>
                x.TenantId == tenantId &&
                mediaIds.Contains(x.Id) &&
                x.Status == "STAGED").ToListAsync(cancellationToken);
            foreach (var item in media.Where(x =>
                         x.SaleId == saleId &&
                         x.SaleLineId == input.SaleLineId &&
                         (!x.OutletId.HasValue || x.OutletId == outletId)))
            {
                item.AttachToDraftLine(draft.Id, line.Id, draft.ExpiresAt);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return new PosReturnInspectionDraftSaveResult(await MapDraftAsync(draft, cancellationToken), null);
    }

    public async Task<bool> MarkInspectionDraftValidatedAsync(
        Guid tenantId, Guid draftId, Guid userId, DateTimeOffset now,
        bool requiresInspection, bool requiresManagerApproval,
        CancellationToken cancellationToken)
    {
        var draft = await _dbContext.ReturnInspectionDrafts.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == draftId, cancellationToken);
        if (draft is null || draft.Status == "CONSUMED" || draft.IsExpired(now)) return false;
        draft.MarkValidated(userId, now, requiresInspection, requiresManagerApproval);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PosReturnInspectionDraftRecord?> GetValidatedInspectionDraftForCompletionAsync(
        Guid tenantId, Guid outletId, Guid saleId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId && x.Status == "VALIDATED", cancellationToken);
        if (draft is null || draft.IsExpired(now))
        {
            return null;
        }

        return await MapDraftAsync(draft, cancellationToken);
    }

    private async Task<PosReturnInspectionDraftRecord> MapDraftAsync(ReturnInspectionDraft draft, CancellationToken cancellationToken)
    {
        var lines = await _dbContext.ReturnInspectionDraftLines.AsNoTracking()
            .Where(x => x.TenantId == draft.TenantId && x.ReturnInspectionDraftId == draft.Id)
            .Select(x => new { x.Id, x.SaleLineId, x.ConditionId, x.ConditionCodeSnapshot, x.InspectionNotes, x.InspectionStatus })
            .ToListAsync(cancellationToken);
        var media = await _dbContext.ReturnInspectionMediaStaging.AsNoTracking().Where(x =>
            x.TenantId == draft.TenantId && x.InspectionDraftId == draft.Id && x.Status == "STAGED")
            .Select(x => new { x.Id, x.InspectionDraftLineId }).ToListAsync(cancellationToken);
        return new PosReturnInspectionDraftRecord(draft.Id, draft.Status, draft.ValidatedAt,
            lines.Select(x => new PosReturnInspectionDraftLineRecord(x.Id, x.SaleLineId, x.ConditionId,
                x.ConditionCodeSnapshot, x.InspectionNotes, x.InspectionStatus,
                media.Where(m => m.InspectionDraftLineId == x.Id).Select(m => m.Id).ToList())).ToList(),
            draft.ResolutionType,
            draft.ResolutionSelectedAt,
            draft.ResolutionSelectedByTenantUserId,
            draft.Version,
            draft.ExpiresAt,
            draft.RequiresInspection,
            draft.RequiresManagerApproval);
    }

    public async Task<PosReturnResolutionSaveRepositoryResult> SaveResolutionAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid userId,
        string resolutionType,
        int expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

        var draft = await _dbContext.ReturnInspectionDrafts.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);
        if (draft is null)
        {
            return new PosReturnResolutionSaveRepositoryResult(null, "draft_not_found");
        }

        if (string.Equals(draft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnResolutionSaveRepositoryResult(null, "stale");
        }

        if (string.Equals(draft.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnResolutionSaveRepositoryResult(null, "stale");
        }

        if (draft.IsExpired(now))
        {
            return new PosReturnResolutionSaveRepositoryResult(null, "expired");
        }

        if (!string.Equals(draft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnResolutionSaveRepositoryResult(null, "inspection_not_validated");
        }

        if (draft.Version != expectedVersion)
        {
            return new PosReturnResolutionSaveRepositoryResult(null, "conflict");
        }

        var changed = draft.SetResolution(resolutionType, userId, now);
        if (changed && !string.Equals(resolutionType, "EXCHANGE", StringComparison.OrdinalIgnoreCase))
        {
            await ClearExchangeReplacementLinesAsync(draft.Id, tenantId, cancellationToken);
        }

        if (changed)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                return new PosReturnResolutionSaveRepositoryResult(null, "conflict");
            }
        }

        return new PosReturnResolutionSaveRepositoryResult(
            MapResolution(draft),
            null);
    }

    public async Task<PosReturnResolutionRecord?> GetResolutionAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);
        if (draft is null)
        {
            return null;
        }

        return MapResolution(draft);
    }

    private static PosReturnResolutionRecord MapResolution(ReturnInspectionDraft draft)
    {
        var canChange = string.Equals(draft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase);
        return new PosReturnResolutionRecord(
            draft.SaleId,
            draft.Id,
            draft.ResolutionType,
            draft.ResolutionSelectedAt,
            draft.ResolutionSelectedByTenantUserId,
            draft.Version,
            draft.Status,
            draft.ExpiresAt,
            draft.RequiresInspection,
            draft.RequiresManagerApproval,
            canChange);
    }

    public async Task<PosReturnRefundMethodsRepositoryResult> GetRefundMethodsAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        bool hasOpenTillSession,
        CancellationToken cancellationToken)
    {
        var draftError = await ValidateRefundWorkflowDraftAsync(
            tenantId, outletId, saleId, cancellationToken);
        if (draftError is not null)
        {
            return new PosReturnRefundMethodsRepositoryResult(null, draftError);
        }

        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .FirstAsync(x => x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId,
                cancellationToken);

        var paymentRows = await LoadRefundablePaymentRowsAsync(tenantId, saleId, cancellationToken);
        var items = new List<PosReturnRefundMethodOptionDto>();

        var cardPayments = paymentRows
            .Where(x => x.SupportsRefund &&
                        (x.MethodType == "CARD" || x.MethodCode.Contains("CARD", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var cardAvailable = cardPayments.Sum(x =>
            Math.Max(0, x.Payment.PaidAmount - x.Payment.RefundedAmount));
        var primaryCard = cardPayments
            .OrderBy(x => x.Payment.PaidAt)
            .ThenBy(x => x.Payment.Id)
            .FirstOrDefault();
        var originalEnabled = cardPayments.Count > 0 && cardAvailable > 0;
        string? originalMasked = null;
        if (primaryCard is not null)
        {
            var sanitized = await _dbContext.SalesPaymentTransactions
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId &&
                            t.SalesPaymentId == primaryCard.Payment.Id &&
                            t.TransactionStatus == "SUCCEEDED")
                .OrderBy(t => t.ProcessedAt)
                .ThenBy(t => t.Id)
                .Select(t => t.ProviderResponseJson)
                .FirstOrDefaultAsync(cancellationToken);
            originalMasked = MaskPaymentReference(
                primaryCard.Payment.ExternalReference,
                sanitized);
        }

        items.Add(new PosReturnRefundMethodOptionDto(
            "ORIGINAL_PAYMENT",
            "Original Payment Method",
            originalEnabled,
            originalEnabled ? null : "Original card payment is not refundable for this sale.",
            primaryCard?.PaymentMethodName,
            originalMasked,
            RequiresOpenTill: false,
            RequiresProvider: true,
            RequiresApproval: false));

        var cashMethod = await _dbContext.PaymentMethods.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.MethodCode == "CASH" &&
                        x.IsActiveForPos &&
                        x.SupportsRefund &&
                        x.Status == "ACTIVE")
            .OrderBy(x => x.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
        var cashEnabled = cashMethod is not null && hasOpenTillSession;
        items.Add(new PosReturnRefundMethodOptionDto(
            "CASH",
            "Cash",
            cashEnabled,
            cashEnabled
                ? null
                : cashMethod is null
                    ? "Cash refunds are not configured for this tenant."
                    : "An open till session is required for cash refunds.",
            null,
            null,
            RequiresOpenTill: true,
            RequiresProvider: false,
            RequiresApproval: false));

        return new PosReturnRefundMethodsRepositoryResult(
            new PosReturnRefundMethodsResponseDto(
                items,
                DefaultMethodCode: null,
                SelectedMethodCode: draft.RefundMethodCode,
                SelectedAt: draft.RefundMethodSelectedAt),
            null);
    }

    public async Task<PosReturnRefundMethodSaveRepositoryResult> SaveRefundMethodAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid userId,
        string methodCode,
        bool hasOpenTillSession,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var draftError = await ValidateRefundWorkflowDraftAsync(
            tenantId, outletId, saleId, cancellationToken);
        if (draftError is not null)
        {
            return new PosReturnRefundMethodSaveRepositoryResult(null, draftError);
        }

        var methodsResult = await GetRefundMethodsAsync(
            tenantId, outletId, saleId, hasOpenTillSession, cancellationToken);
        if (methodsResult.Methods is null)
        {
            return new PosReturnRefundMethodSaveRepositoryResult(null, methodsResult.ErrorCode ?? "method_not_allowed");
        }

        var allowed = methodsResult.Methods.Items.FirstOrDefault(x =>
            string.Equals(x.Code, methodCode, StringComparison.OrdinalIgnoreCase));
        if (allowed is null)
        {
            return new PosReturnRefundMethodSaveRepositoryResult(null, "invalid_refund_method");
        }

        if (!allowed.Enabled)
        {
            return new PosReturnRefundMethodSaveRepositoryResult(null, "refund_method_not_allowed");
        }

        var draft = await _dbContext.ReturnInspectionDrafts.FirstAsync(x =>
            x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);
        draft.SetRefundMethod(methodCode.Trim().ToUpperInvariant(), userId, now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PosReturnRefundMethodSaveRepositoryResult(
            new PosReturnRefundMethodRecord(saleId, draft.RefundMethodCode!, now),
            null);
    }

    public async Task<PosReturnRefundMethodRecord?> GetSavedRefundMethodAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);
        if (draft is null
            || string.IsNullOrWhiteSpace(draft.RefundMethodCode)
            || draft.RefundMethodSelectedAt is null)
        {
            return null;
        }

        return new PosReturnRefundMethodRecord(
            saleId,
            draft.RefundMethodCode,
            draft.RefundMethodSelectedAt.Value);
    }

    public async Task<string?> GetSaleCurrencyCodeAsync(
        Guid tenantId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        return await (
            from order in _dbContext.SalesOrders.AsNoTracking()
            join receipt in _dbContext.Receipts.AsNoTracking()
                on order.Id equals receipt.SalesOrderId
            where order.TenantId == tenantId &&
                  order.Id == saleId &&
                  receipt.TenantId == tenantId &&
                  receipt.ReceiptType == "SALE"
            select receipt.CurrencyCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PosExchangeReplacementSaveRepositoryResult> SaveExchangeReplacementAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid userId,
        IReadOnlyList<PosExchangeReplacementItemRequestDto> items,
        int expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var draftError = await ValidateExchangeWorkflowDraftAsync(
            tenantId, outletId, saleId, cancellationToken, now);
        if (draftError is not null)
        {
            return new PosExchangeReplacementSaveRepositoryResult(null, draftError);
        }

        if (items is null || items.Count == 0)
        {
            return new PosExchangeReplacementSaveRepositoryResult(null, "invalid_replacement");
        }

        if (items.Any(x =>
                x.ReturnedSaleLineId == Guid.Empty ||
                x.ReplacementProductId == Guid.Empty ||
                x.ReplacementVariantId == Guid.Empty ||
                x.Quantity <= 0))
        {
            return new PosExchangeReplacementSaveRepositoryResult(null, "invalid_replacement");
        }

        var draft = await _dbContext.ReturnInspectionDrafts.FirstAsync(x =>
            x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);

        if (draft.Version != expectedVersion)
        {
            return new PosExchangeReplacementSaveRepositoryResult(null, "conflict");
        }

        var validation = await ValidateReplacementItemsAsync(
            tenantId, outletId, saleId, items, cancellationToken);
        if (validation.ErrorCode is not null)
        {
            return new PosExchangeReplacementSaveRepositoryResult(null, validation.ErrorCode);
        }

        await ClearExchangeReplacementLinesAsync(draft.Id, tenantId, cancellationToken);

        foreach (var item in items)
        {
            _dbContext.ReturnExchangeReplacementDraftLines.Add(
                ReturnExchangeReplacementDraftLine.Create(
                    Guid.NewGuid(),
                    tenantId,
                    draft.Id,
                    item.ReturnedSaleLineId,
                    item.ReplacementProductId,
                    item.ReplacementVariantId,
                    item.Quantity,
                    userId,
                    now));
        }

        draft.MarkExchangeReplacementChanged();
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await BuildExchangeReplacementResponseAsync(
            tenantId, outletId, saleId, now, cancellationToken);
        if (saved is null)
        {
            return new PosExchangeReplacementSaveRepositoryResult(null, "replacement_save_failed");
        }

        return new PosExchangeReplacementSaveRepositoryResult(saved, null);
    }

    public async Task<PosExchangeReplacementRepositoryResult> GetExchangeReplacementAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var draftError = await ValidateExchangeWorkflowDraftAsync(
            tenantId, outletId, saleId, cancellationToken, now);
        if (draftError is not null)
        {
            return new PosExchangeReplacementRepositoryResult(null, draftError);
        }

        var replacement = await BuildExchangeReplacementResponseAsync(
            tenantId, outletId, saleId, now, cancellationToken);
        if (replacement is null || replacement.Items.Count == 0)
        {
            return new PosExchangeReplacementRepositoryResult(null, "replacement_not_found");
        }

        return new PosExchangeReplacementRepositoryResult(replacement, null);
    }

    public async Task<PosExchangePreviewRepositoryResult> PreviewExchangeAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        string reasonCode,
        IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var draftError = await ValidateExchangeWorkflowDraftAsync(
            tenantId, outletId, saleId, cancellationToken, now);
        if (draftError is not null)
        {
            return new PosExchangePreviewRepositoryResult(null, draftError);
        }

        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .FirstAsync(x =>
                x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId,
                cancellationToken);

        var creditResult = await PreviewCreditAsync(
            tenantId, saleId, reasonCode, lines, now, cancellationToken);
        if (creditResult.Preview is null)
        {
            return new PosExchangePreviewRepositoryResult(null, creditResult.ErrorCode ?? "preview_failed");
        }

        var replacement = await BuildExchangeReplacementResponseAsync(
            tenantId, outletId, saleId, now, cancellationToken);
        if (replacement is null || replacement.Items.Count == 0)
        {
            return new PosExchangePreviewRepositoryResult(null, "replacement_not_found");
        }

        var pricing = await CalculateExchangeReplacementPricingAsync(
            tenantId, outletId, replacement.Items, now, cancellationToken);
        if (!pricing.IsSuccess)
        {
            return pricing.ErrorCode switch
            {
                "insufficient_outlet_stock" => BuildBlockedPreview(
                    saleId,
                    creditResult.Preview,
                    replacement,
                    draft.Version,
                    canProceed: false,
                    requiresApproval: draft.RequiresManagerApproval || creditResult.Preview.RequiresApproval,
                    ["Insufficient outlet stock for one or more replacement items."],
                    pricing),
                "price_not_found" => BuildBlockedPreview(
                    saleId,
                    creditResult.Preview,
                    replacement,
                    draft.Version,
                    canProceed: false,
                    requiresApproval: draft.RequiresManagerApproval || creditResult.Preview.RequiresApproval,
                    ["A current price is unavailable for one or more replacement items."],
                    pricing),
                _ => new PosExchangePreviewRepositoryResult(null, pricing.ErrorCode),
            };
        }

        var pricedItems = MapPricedReplacementItems(replacement.Items, pricing);
        var policyMessages = new List<string>();
        var canProceed = true;
        string? blockingCode = null;

        foreach (var item in pricedItems)
        {
            if (item.Quantity <= 0)
            {
                canProceed = false;
                blockingCode ??= "exchange_invalid_quantity";
                policyMessages.Add("Replacement quantity must be greater than zero.");
            }

            if (string.Equals(item.StockStatus, "OutOfStock", StringComparison.OrdinalIgnoreCase) ||
                (item.AvailableQuantity.HasValue && item.AvailableQuantity.Value < item.Quantity))
            {
                canProceed = false;
                blockingCode ??= "insufficient_outlet_stock";
                policyMessages.Add($"Insufficient outlet stock for {item.ProductName}.");
            }

            if (item.UnitPrice < 0 || string.IsNullOrWhiteSpace(item.CurrencyCode))
            {
                canProceed = false;
                blockingCode ??= "exchange_price_unavailable";
                policyMessages.Add($"A current price is unavailable for {item.ProductName}.");
            }

            if (!string.IsNullOrWhiteSpace(creditResult.Preview.Currency) &&
                !string.IsNullOrWhiteSpace(item.CurrencyCode) &&
                !string.Equals(
                    creditResult.Preview.Currency,
                    item.CurrencyCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                canProceed = false;
                blockingCode ??= "exchange_currency_mismatch";
                policyMessages.Add("Replacement currency does not match the original sale currency.");
            }
        }

        if (!string.IsNullOrWhiteSpace(pricing.CurrencyCode) &&
            !string.Equals(
                creditResult.Preview.Currency,
                pricing.CurrencyCode,
                StringComparison.OrdinalIgnoreCase))
        {
            canProceed = false;
            blockingCode ??= "exchange_currency_mismatch";
            policyMessages.Add("Replacement currency does not match the original sale currency.");
        }

        var returnValue = creditResult.Preview.Calculation.NetCreditAmount;
        var replacementSubtotal = pricing.Subtotal;
        var replacementDiscount = pricing.DiscountTotal;
        var replacementTax = pricing.TaxTotal;
        var replacementValue = pricing.GrandTotal;
        var difference = RoundAmount(replacementValue - returnValue);
        var direction = difference switch
        {
            > 0 => "CUSTOMER_PAYS",
            < 0 => "CUSTOMER_RECEIVES",
            _ => "EVEN_EXCHANGE",
        };

        var requiresApproval =
            draft.RequiresManagerApproval ||
            creditResult.Preview.RequiresApproval;

        if (requiresApproval)
        {
            canProceed = false;
            blockingCode ??= "exchange_approval_required";
            policyMessages.Add(
                "Manager approval is required before this exchange can be completed.");
        }

        var amountDueFromCustomer = difference > 0 ? difference : 0m;
        var amountDueToCustomer = difference < 0 ? Math.Abs(difference) : 0m;

        var preview = new PosExchangePreviewDto(
            saleId,
            creditResult.Preview.Currency,
            creditResult.Preview.SelectedItemCount,
            returnValue,
            replacementValue,
            TaxAdjustment: replacementTax,
            DiscountAdjustment: replacementDiscount,
            Math.Abs(difference),
            direction,
            canProceed,
            requiresApproval,
            policyMessages,
            pricedItems,
            ReplacementSubtotal: replacementSubtotal,
            ReplacementDiscount: replacementDiscount,
            ReplacementTax: replacementTax,
            AmountDueFromCustomer: amountDueFromCustomer,
            AmountDueToCustomer: amountDueToCustomer,
            DraftVersion: draft.Version);

        if (!canProceed &&
            blockingCode is "insufficient_outlet_stock" or "exchange_price_unavailable" or
                "exchange_currency_mismatch" or "exchange_invalid_quantity" or
                "exchange_approval_required" or "exchange_settlement_unsupported")
        {
            return new PosExchangePreviewRepositoryResult(preview, null);
        }

        if (!canProceed)
        {
            return new PosExchangePreviewRepositoryResult(null, blockingCode ?? "exchange_cannot_proceed");
        }

        return new PosExchangePreviewRepositoryResult(preview, null);
    }

    private static PosExchangePreviewRepositoryResult BuildBlockedPreview(
        Guid saleId,
        PosReturnCreditPreviewDto credit,
        PosExchangeReplacementSaveResponseDto replacement,
        int draftVersion,
        bool canProceed,
        bool requiresApproval,
        IReadOnlyList<string> policyMessages,
        PosSaleLinePricingResult pricing)
    {
        var returnValue = credit.Calculation.NetCreditAmount;
        var replacementValue = pricing.IsSuccess ? pricing.GrandTotal : 0m;
        var difference = RoundAmount(replacementValue - returnValue);
        var direction = difference switch
        {
            > 0 => "CUSTOMER_PAYS",
            < 0 => "CUSTOMER_RECEIVES",
            _ => "EVEN_EXCHANGE",
        };

        return new PosExchangePreviewRepositoryResult(
            new PosExchangePreviewDto(
                saleId,
                credit.Currency,
                credit.SelectedItemCount,
                returnValue,
                replacementValue,
                TaxAdjustment: pricing.TaxTotal,
                DiscountAdjustment: pricing.DiscountTotal,
                Math.Abs(difference),
                direction,
                canProceed,
                requiresApproval,
                policyMessages,
                replacement.Items,
                ReplacementSubtotal: pricing.Subtotal,
                ReplacementDiscount: pricing.DiscountTotal,
                ReplacementTax: pricing.TaxTotal,
                AmountDueFromCustomer: difference > 0 ? difference : 0m,
                AmountDueToCustomer: difference < 0 ? Math.Abs(difference) : 0m,
                DraftVersion: draftVersion),
            null);
    }

    private async Task<PosSaleLinePricingResult> CalculateExchangeReplacementPricingAsync(
        Guid tenantId,
        Guid outletId,
        IReadOnlyList<PosExchangeReplacementItemDto> items,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var requests = items
            .Select(item => new PosSaleLinePricingRequest(
                item.ReturnedSaleLineId,
                item.ReplacementProductId,
                item.ReplacementVariantId,
                item.Quantity))
            .ToList();

        return await _saleLinePricingCalculator.CalculateAsync(
            tenantId,
            outletId,
            requests,
            now,
            cancellationToken);
    }

    private static IReadOnlyList<PosExchangeReplacementItemDto> MapPricedReplacementItems(
        IReadOnlyList<PosExchangeReplacementItemDto> sourceItems,
        PosSaleLinePricingResult pricing)
    {
        var byKey = pricing.Lines.ToDictionary(x => x.LineKey);
        var mapped = new List<PosExchangeReplacementItemDto>(sourceItems.Count);
        foreach (var item in sourceItems)
        {
            if (!byKey.TryGetValue(item.ReturnedSaleLineId, out var priced))
            {
                mapped.Add(item);
                continue;
            }

            mapped.Add(item with
            {
                UnitPrice = priced.UnitPrice,
                LineTotal = priced.LineTotal,
                AvailableQuantity = priced.AvailableQuantity,
                StockStatus = priced.AvailableQuantity <= 0 ? "OutOfStock" : "InStock",
                LineSubtotal = priced.LineSubtotal,
                LineDiscount = priced.LineDiscount,
                LineTax = priced.LineTax,
                PriceListItemId = priced.PriceListItemId,
                CurrencyCode = pricing.CurrencyCode ?? item.CurrencyCode,
            });
        }

        return mapped;
    }

    private async Task ClearExchangeReplacementLinesAsync(
        Guid draftId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ReturnExchangeReplacementDraftLines
            .Where(x => x.TenantId == tenantId && x.ReturnInspectionDraftId == draftId)
            .ToListAsync(cancellationToken);
        if (existing.Count == 0)
        {
            return;
        }

        _dbContext.ReturnExchangeReplacementDraftLines.RemoveRange(existing);
    }

    private async Task<(string? ErrorCode, IReadOnlyDictionary<Guid, ReplacementPricingRow> Pricing)>
        ValidateReplacementItemsAsync(
            Guid tenantId,
            Guid outletId,
            Guid saleId,
            IReadOnlyList<PosExchangeReplacementItemRequestDto> items,
            CancellationToken cancellationToken)
    {
        if (items.Any(x =>
                x.ReturnedSaleLineId == Guid.Empty ||
                x.ReplacementProductId == Guid.Empty ||
                x.ReplacementVariantId == Guid.Empty ||
                x.Quantity <= 0))
        {
            return ("invalid_replacement", new Dictionary<Guid, ReplacementPricingRow>());
        }

        if (items.Select(x => x.ReturnedSaleLineId).Distinct().Count() != items.Count)
        {
            return ("invalid_replacement", new Dictionary<Guid, ReplacementPricingRow>());
        }

        foreach (var item in items)
        {
            var belongs = await SaleLineBelongsToSaleAsync(
                tenantId, saleId, item.ReturnedSaleLineId, cancellationToken);
            if (!belongs)
            {
                return ("sale_line_not_found", new Dictionary<Guid, ReplacementPricingRow>());
            }
        }

        var variantIds = items.Select(x => x.ReplacementVariantId).Distinct().ToArray();
        var variants = await _dbContext.ProductVariants.AsNoTracking()
            .Where(x => x.TenantId == tenantId && variantIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        if (variants.Count != variantIds.Length)
        {
            return ("variant_not_found", new Dictionary<Guid, ReplacementPricingRow>());
        }

        var productIds = variants.Select(x => x.ProductId).Distinct().ToArray();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (products.Count != productIds.Length)
        {
            return ("product_not_found", new Dictionary<Guid, ReplacementPricingRow>());
        }

        foreach (var variant in variants)
        {
            if (!products.TryGetValue(variant.ProductId, out var product) ||
                !string.Equals(product.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                !product.IsSellable ||
                !variant.IsSellable ||
                string.Equals(variant.Status, "DELETED", StringComparison.OrdinalIgnoreCase))
            {
                return ("product_not_sellable", new Dictionary<Guid, ReplacementPricingRow>());
            }
        }

        var defaultPriceListId = await _dbContext.PriceLists.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!defaultPriceListId.HasValue)
        {
            return ("price_not_found", new Dictionary<Guid, ReplacementPricingRow>());
        }

        var prices = await _dbContext.PriceListItems.AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.PriceListId == defaultPriceListId.Value &&
                x.ProductVariantId.HasValue &&
                variantIds.Contains(x.ProductVariantId.Value) &&
                x.Status == "ACTIVE")
            .Select(x => new { VariantId = x.ProductVariantId!.Value, x.SellingPrice })
            .ToDictionaryAsync(x => x.VariantId, x => x.SellingPrice, cancellationToken);
        if (prices.Count != variantIds.Length)
        {
            return ("price_not_found", new Dictionary<Guid, ReplacementPricingRow>());
        }

        var inventory = await LoadOutletAvailableStockAsync(
            tenantId,
            outletId,
            variantIds,
            cancellationToken);

        var pricing = new Dictionary<Guid, ReplacementPricingRow>();
        foreach (var item in items)
        {
            var variant = variants.First(x => x.Id == item.ReplacementVariantId);
            var product = products[variant.ProductId];
            inventory.TryGetValue(variant.Id, out var availableQty);
            if (availableQty < item.Quantity)
            {
                return ("insufficient_stock", new Dictionary<Guid, ReplacementPricingRow>());
            }

            pricing[item.ReturnedSaleLineId] = new ReplacementPricingRow(
                product,
                variant,
                prices[variant.Id],
                availableQty);
        }

        return (null, pricing);
    }

    private async Task<PosExchangeReplacementSaveResponseDto?> BuildExchangeReplacementResponseAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);
        if (draft is null)
        {
            return null;
        }

        var lines = await _dbContext.ReturnExchangeReplacementDraftLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ReturnInspectionDraftId == draft.Id)
            .OrderBy(x => x.SelectedAt)
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
        {
            return null;
        }

        var currency = await GetSaleCurrencyCodeAsync(tenantId, saleId, cancellationToken) ?? string.Empty;
        var variantIds = lines.Select(x => x.ReplacementVariantId).Distinct().ToArray();
        var variants = await _dbContext.ProductVariants.AsNoTracking()
            .Where(x => x.TenantId == tenantId && variantIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var productIds = variants.Values.Select(x => x.ProductId).Distinct().ToArray();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var defaultPriceListId = await _dbContext.PriceLists.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var prices = defaultPriceListId.HasValue
            ? await _dbContext.PriceListItems.AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PriceListId == defaultPriceListId.Value &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == "ACTIVE")
                .Select(x => new { VariantId = x.ProductVariantId!.Value, x.SellingPrice })
                .ToDictionaryAsync(x => x.VariantId, x => x.SellingPrice, cancellationToken)
            : new Dictionary<Guid, decimal>();

        var inventory = await LoadOutletAvailableStockAsync(
            tenantId,
            outletId,
            variantIds,
            cancellationToken);

        var imageRows = await _dbContext.ProductImages.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId) && x.Status == "ACTIVE")
            .OrderBy(x => x.IsPrimaryImage ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new { x.ProductId, x.ImageStorageKey, x.ImageUrl })
            .ToListAsync(cancellationToken);
        var imageByProduct = imageRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First().ImageUrl ?? x.First().ImageStorageKey);

        var items = new List<PosExchangeReplacementItemDto>(lines.Count);
        foreach (var line in lines)
        {
            if (!variants.TryGetValue(line.ReplacementVariantId, out var variant) ||
                !products.TryGetValue(variant.ProductId, out var product) ||
                !prices.TryGetValue(variant.Id, out var unitPrice))
            {
                continue;
            }

            inventory.TryGetValue(variant.Id, out var availableQty);
            imageByProduct.TryGetValue(product.Id, out var imageKey);
            var lineTotal = RoundAmount(unitPrice * line.Quantity);
            var stockStatus = availableQty <= 0 ? "OutOfStock" : "InStock";

            items.Add(new PosExchangeReplacementItemDto(
                line.ReturnedSaleLineId,
                line.ReplacementProductId,
                line.ReplacementVariantId,
                product.ProductName,
                variant.Sku ?? string.Empty,
                variant.VariantName,
                imageKey,
                line.Quantity,
                unitPrice,
                lineTotal,
                currency,
                stockStatus,
                availableQty,
                line.SelectedAt));
        }

        if (items.Count == 0)
        {
            return null;
        }

        return new PosExchangeReplacementSaveResponseDto(
            saleId,
            items,
            items.Max(x => x.SelectedAt),
            draft.Version,
            draft.ExpiresAt);
    }

    private async Task<string?> ValidateExchangeWorkflowDraftAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken,
        DateTimeOffset? now = null)
    {
        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);
        if (draft is null)
        {
            return "draft_not_found";
        }

        if (string.Equals(draft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(draft.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            return "stale";
        }

        if (!string.Equals(draft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase))
        {
            return "inspection_not_validated";
        }

        if (!string.Equals(draft.ResolutionType, "EXCHANGE", StringComparison.OrdinalIgnoreCase))
        {
            return "invalid_resolution";
        }

        if (now.HasValue && draft.IsExpired(now.Value))
        {
            return "expired";
        }

        return null;
    }

    private async Task<Dictionary<Guid, decimal>> LoadOutletAvailableStockAsync(
        Guid tenantId,
        Guid outletId,
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var rows = await (
                from balance in _dbContext.InventoryBalances.AsNoTracking()
                join location in _dbContext.InventoryLocations.AsNoTracking()
                    on balance.InventoryLocationId equals location.Id
                where balance.TenantId == tenantId &&
                      location.TenantId == tenantId &&
                      location.OutletId == outletId &&
                      location.IsSellableLocation &&
                      location.Status == "ACTIVE" &&
                      balance.ProductVariantId.HasValue &&
                      variantIds.Contains(balance.ProductVariantId.Value)
                group balance by balance.ProductVariantId!.Value
                into grouped
                select new
                {
                    VariantId = grouped.Key,
                    Available = grouped.Sum(x => x.AvailableQuantity),
                })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.VariantId, x => x.Available);
    }

    private sealed record ReplacementPricingRow(
        E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.Product Product,
        E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ProductVariant Variant,
        decimal UnitPrice,
        decimal AvailableQuantity);

    private async Task<string?> ValidateRefundWorkflowDraftAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var draft = await _dbContext.ReturnInspectionDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.OutletId == outletId && x.SaleId == saleId, cancellationToken);
        if (draft is null)
        {
            return "draft_not_found";
        }

        if (string.Equals(draft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(draft.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            return "stale";
        }

        if (!string.Equals(draft.Status, "VALIDATED", StringComparison.OrdinalIgnoreCase))
        {
            return "inspection_not_validated";
        }

        if (!string.Equals(draft.ResolutionType, "REFUND", StringComparison.OrdinalIgnoreCase))
        {
            return "invalid_resolution";
        }

        return null;
    }

    private async Task<List<PaymentAllocationRow>> LoadRefundablePaymentRowsAsync(
        Guid tenantId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var payments = await _dbContext.SalesPayments.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.SalesOrderId == saleId &&
                        (x.PaymentStatus == "PAID" || x.PaymentStatus == "PARTIALLY_REFUNDED"))
            .ToListAsync(cancellationToken);
        if (payments.Count == 0)
        {
            return [];
        }

        var methodIds = payments.Select(x => x.PaymentMethodId).Distinct().ToArray();
        var paymentMethods = await _dbContext.PaymentMethods.AsNoTracking()
            .Where(x => x.TenantId == tenantId && methodIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return payments
            .Where(payment => paymentMethods.ContainsKey(payment.PaymentMethodId))
            .Select(payment =>
            {
                var method = paymentMethods[payment.PaymentMethodId];
                return new PaymentAllocationRow(
                    payment,
                    method.Id,
                    method.MethodCode,
                    method.MethodName,
                    method.MethodType,
                    method.SupportsRefund);
            })
            .ToList();
    }

    private async Task<Dictionary<Guid, PaymentDisplay>> LoadPaymentDisplaysAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> saleIds,
        CancellationToken cancellationToken)
    {
        if (saleIds.Count == 0)
        {
            return [];
        }

        var rows = await (
            from payment in _dbContext.SalesPayments.AsNoTracking()
            join method in _dbContext.PaymentMethods.AsNoTracking()
                on payment.PaymentMethodId equals method.Id
            where payment.TenantId == tenantId &&
                  method.TenantId == tenantId &&
                  saleIds.Contains(payment.SalesOrderId) &&
                  (payment.PaymentStatus == "PAID" ||
                   payment.PaymentStatus == "PARTIALLY_REFUNDED")
            orderby payment.SalesOrderId, payment.PaidAt, payment.Id
            select new PaymentDisplayRow(
                payment.Id,
                payment.SalesOrderId,
                method.MethodCode,
                method.MethodName,
                method.MethodType,
                payment.ExternalReference))
            .ToListAsync(cancellationToken);

        var paymentIds = rows.Select(x => x.PaymentId).Distinct().ToList();
        var sanitizedByPaymentId = await LoadSanitizedCardMetadataByPaymentIdAsync(
            tenantId,
            paymentIds,
            cancellationToken);

        return rows
            .GroupBy(row => row.SaleId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var ordered = group.ToList();
                    if (ordered.Count > 1)
                    {
                        return new PaymentDisplay("Multiple", null);
                    }

                    var payment = ordered[0];
                    sanitizedByPaymentId.TryGetValue(payment.PaymentId, out var sanitizedJson);
                    return BuildPaymentDisplay(
                        payment.MethodCode,
                        payment.MethodName,
                        payment.MethodType,
                        payment.ExternalReference,
                        sanitizedJson);
                });
    }

    private async Task<Dictionary<Guid, string?>> LoadSanitizedCardMetadataByPaymentIdAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> paymentIds,
        CancellationToken cancellationToken)
    {
        if (paymentIds.Count == 0)
        {
            return [];
        }

        var transactions = await _dbContext.SalesPaymentTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId &&
                        paymentIds.Contains(t.SalesPaymentId) &&
                        t.TransactionStatus == "SUCCEEDED")
            .OrderBy(t => t.ProcessedAt)
            .ThenBy(t => t.Id)
            .Select(t => new { t.SalesPaymentId, t.ProviderResponseJson })
            .ToListAsync(cancellationToken);

        return transactions
            .GroupBy(t => t.SalesPaymentId)
            .ToDictionary(
                g => g.Key,
                g => g.First().ProviderResponseJson);
    }

    private static PaymentDisplay BuildPaymentDisplay(
        string methodCode,
        string methodName,
        string methodType,
        string? externalReference,
        string? sanitizedProviderJson)
    {
        var isCard = string.Equals(methodType, "CARD", StringComparison.OrdinalIgnoreCase);
        string? brand = null;
        string? last4 = null;
        if (isCard)
        {
            (brand, _) = SafePaymentDisplay.TryParseSanitizedCardMetadata(sanitizedProviderJson);
            last4 = SafePaymentDisplay.ResolveLast4(sanitizedProviderJson, externalReference);
        }

        var method = SafePaymentDisplay.ResolveMethodLabel(methodName, methodCode, isCard ? brand : null);
        var maskedReference = isCard
            ? SafePaymentDisplay.FormatMaskedReference(last4)
            : null;

        return new PaymentDisplay(method, maskedReference);
    }

    private static IReadOnlyList<string> BuildPhoneSearchValues(string? search)
    {
        var normalized = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer
            .NormalizePhone(search ?? string.Empty);
        var values = new List<string> { normalized };

        if (normalized.StartsWith("+94", StringComparison.Ordinal) &&
            normalized.Length > 3)
        {
            values.Add($"0{normalized[3..]}");
        }
        else if (normalized.StartsWith("94", StringComparison.Ordinal) &&
                 normalized.Length > 2)
        {
            values.Add($"0{normalized[2..]}");
        }
        else if (normalized.StartsWith('0') && normalized.Length > 1)
        {
            values.Add($"+94{normalized[1..]}");
        }

        return values.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string? MaskPaymentReference(string? reference, string? sanitizedProviderJson = null)
    {
        var last4 = SafePaymentDisplay.ResolveLast4(sanitizedProviderJson, reference);
        return SafePaymentDisplay.FormatMaskedReference(last4);
    }

    public async Task<PosReturnInspectionMediaStagingRecord?> GetInspectionMediaStagingAsync(
        Guid tenantId,
        Guid outletId,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ReturnInspectionMediaStaging
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Id == mediaId &&
                        x.Status == "STAGED" &&
                        x.OutletId == outletId)
            .Select(x => new PosReturnInspectionMediaStagingRecord(
                x.Id,
                x.SaleId,
                x.SaleLineId,
                x.StorageKey,
                x.FileName,
                x.ContentType,
                x.SizeBytes,
                x.Status,
                x.InspectionDraftId,
                x.InspectionDraftLineId,
                x.ExpiresAt,
                x.OutletId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PosReturnInspectionMediaStagingRecord>> GetInspectionMediaForSaleLineAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid saleLineId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ReturnInspectionMediaStaging
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.OutletId == outletId &&
                        x.SaleId == saleId &&
                        x.SaleLineId == saleLineId &&
                        x.Status == "STAGED")
            .OrderBy(x => x.CreatedAt)
            .Select(x => new PosReturnInspectionMediaStagingRecord(
                x.Id,
                x.SaleId,
                x.SaleLineId,
                x.StorageKey,
                x.FileName,
                x.ContentType,
                x.SizeBytes,
                x.Status,
                x.InspectionDraftId,
                x.InspectionDraftLineId,
                x.ExpiresAt,
                x.OutletId))
            .ToListAsync(cancellationToken);
    }

    public async Task<PosReturnInspectionMediaDeleteResult> DeleteInspectionMediaStagingAsync(
        Guid tenantId,
        Guid outletId,
        Guid mediaId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ReturnInspectionMediaStaging
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == mediaId,
                cancellationToken);
        if (entity is null || entity.OutletId != outletId)
        {
            return new PosReturnInspectionMediaDeleteResult(false, null, "pos_returns.media_not_found");
        }

        if (string.Equals(entity.Status, "DELETED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnInspectionMediaDeleteResult(false, null, "pos_returns.media_not_found");
        }

        if (string.Equals(entity.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnInspectionMediaDeleteResult(
                false,
                null,
                "pos_returns.inspection_media_consumed");
        }

        if (!string.Equals(entity.Status, "STAGED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReturnInspectionMediaDeleteResult(false, null, "pos_returns.media_not_found");
        }

        var saleOk = await SaleBelongsToOutletAsync(tenantId, outletId, entity.SaleId, cancellationToken);
        if (!saleOk)
        {
            return new PosReturnInspectionMediaDeleteResult(false, null, "pos_returns.media_not_found");
        }

        var storageKey = entity.StorageKey;
        entity.MarkDeleted(now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new PosReturnInspectionMediaDeleteResult(true, storageKey, null);
    }

    public async Task<PosReturnSaleSearchPageDto> SearchOriginalSalesAsync(
        Guid tenantId,
        Guid outletId,
        string searchType,
        string? search,
        PosReturnSaleSearchFilterDto filters,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query =
            from order in _dbContext.SalesOrders.AsNoTracking()
            join receipt in _dbContext.Receipts.AsNoTracking()
                on order.Id equals receipt.SalesOrderId
            join customer in _dbContext.Customers.AsNoTracking()
                on order.CustomerId equals customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            where order.TenantId == tenantId &&
                  receipt.TenantId == tenantId &&
                  receipt.OutletId == outletId &&
                  order.OrderType == "POS_SALE" &&
                  order.Status == "COMPLETED" &&
                  (order.PaymentStatus == "PAID" ||
                   order.PaymentStatus == "PARTIALLY_REFUNDED") &&
                  receipt.ReceiptType == "SALE" &&
                  receipt.ReceiptStatus == "ISSUED"
            select new { Order = order, Receipt = receipt, Customer = customer };

        if (searchType != "recent")
        {
            var normalizedSearch = search?.Trim().ToLowerInvariant() ?? string.Empty;
            if (searchType == "mobile")
            {
                var phoneValues = BuildPhoneSearchValues(search);
                var primaryPhone = phoneValues[0];
                var alternatePhone = phoneValues.Count > 1
                    ? phoneValues[1]
                    : primaryPhone;
                query = query.Where(x =>
                    (x.Order.CustomerPhoneSnapshot != null &&
                     (x.Order.CustomerPhoneSnapshot
                         .Replace(" ", string.Empty)
                         .Replace("-", string.Empty)
                         .Contains(primaryPhone) ||
                      x.Order.CustomerPhoneSnapshot
                         .Replace(" ", string.Empty)
                         .Replace("-", string.Empty)
                         .Contains(alternatePhone))) ||
                    (x.Customer != null && x.Customer.NormalizedPhone != null &&
                     (x.Customer.NormalizedPhone.Contains(primaryPhone) ||
                      x.Customer.NormalizedPhone.Contains(alternatePhone))));
            }
            else
            {
                query = searchType switch
                {
                    "invoice" => query.Where(x =>
                    x.Receipt.ReceiptNumber.ToLower().Contains(normalizedSearch) ||
                    x.Order.OrderNumber.ToLower().Contains(normalizedSearch)),
                    "sale" => query.Where(x =>
                    x.Order.OrderNumber.ToLower().Contains(normalizedSearch)),
                    "customer" => query.Where(x =>
                    (x.Order.CustomerNameSnapshot != null &&
                     x.Order.CustomerNameSnapshot.ToLower().Contains(normalizedSearch)) ||
                    (x.Customer != null &&
                     x.Customer.Name.ToLower().Contains(normalizedSearch))),
                    _ => query
                };
            }
        }

        if (filters.FromDate.HasValue)
        {
            query = query.Where(x => x.Receipt.BusinessDate >= filters.FromDate.Value);
        }
        if (filters.ToDate.HasValue)
        {
            query = query.Where(x => x.Receipt.BusinessDate <= filters.ToDate.Value);
        }
        if (filters.MinAmount.HasValue)
        {
            query = query.Where(x => x.Receipt.TotalAmount >= filters.MinAmount.Value);
        }
        if (filters.MaxAmount.HasValue)
        {
            query = query.Where(x => x.Receipt.TotalAmount <= filters.MaxAmount.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.PaymentMethodCode))
        {
            var methodIds = _dbContext.PaymentMethods
                .AsNoTracking()
                .Where(method =>
                    method.TenantId == tenantId &&
                    method.MethodCode == filters.PaymentMethodCode &&
                    method.IsActiveForPos &&
                    method.Status == "ACTIVE")
                .Select(method => method.Id);
            query = query.Where(x => _dbContext.SalesPayments.Any(payment =>
                payment.TenantId == tenantId &&
                payment.SalesOrderId == x.Order.Id &&
                methodIds.Contains(payment.PaymentMethodId) &&
                (payment.PaymentStatus == "PAID" ||
                 payment.PaymentStatus == "PARTIALLY_REFUNDED")));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.Receipt.IssuedAt)
            .ThenByDescending(x => x.Order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SearchSaleRow(
                x.Order.Id,
                x.Receipt.ReceiptNumber,
                x.Order.CustomerId,
                x.Order.CustomerNameSnapshot ??
                    (x.Customer != null ? x.Customer.Name : string.Empty) ?? string.Empty,
                x.Order.CustomerPhoneSnapshot ??
                    (x.Customer != null ? x.Customer.Phone : null) ?? string.Empty,
                x.Order.CompletedAt ?? x.Receipt.IssuedAt,
                x.Receipt.TotalAmount,
                _dbContext.SalesOrderLines.Count(line =>
                    line.TenantId == tenantId && line.SalesOrderId == x.Order.Id),
                x.Receipt.CurrencyCode))
            .ToListAsync(cancellationToken);

        var paymentDisplays = await LoadPaymentDisplaysAsync(
            tenantId,
            rows.Select(row => row.SaleId).ToArray(),
            cancellationToken);
        var items = rows.Select(row =>
        {
            var payment = paymentDisplays.GetValueOrDefault(
                row.SaleId,
                PaymentDisplay.Empty);
            return new PosReturnSaleSummaryDto(
                row.SaleId,
                row.InvoiceNo,
                row.CustomerId,
                row.CustomerName,
                row.Phone,
                payment.Method,
                payment.MaskedReference ?? string.Empty,
                row.SaleDate,
                row.Total,
                row.ItemCount,
                row.Currency);
        }).ToList();

        var paymentMethods = await _dbContext.PaymentMethods
            .AsNoTracking()
            .Where(method =>
                method.TenantId == tenantId &&
                method.IsActiveForPos &&
                method.Status == "ACTIVE")
            .OrderBy(method => method.SortOrder)
            .ThenBy(method => method.MethodCode)
            .Select(method => new PosReturnPaymentMethodFilterOptionDto(
                method.MethodCode,
                method.MethodName))
            .ToListAsync(cancellationToken);

        return new PosReturnSaleSearchPageDto(
            items,
            page,
            pageSize,
            totalCount,
            paymentMethods);
    }

    public Task<bool> IsActivePaymentMethodAsync(
        Guid tenantId,
        string paymentMethodCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.PaymentMethods.AsNoTracking().AnyAsync(
            method =>
                method.TenantId == tenantId &&
                method.MethodCode == paymentMethodCode &&
                method.IsActiveForPos &&
                method.Status == "ACTIVE",
            cancellationToken);
    }

    private static (string Status, string? Reason) GetEligibilityStatus(
        bool hasPolicy,
        bool withinWindow,
        decimal availableQty)
    {
        if (!hasPolicy)
        {
            return ("NO_POLICY", "No active return policy is assigned to this product.");
        }

        if (!withinWindow)
        {
            return ("WINDOW_EXPIRED", "The return window for this item has expired.");
        }

        if (availableQty <= 0)
        {
            return ("FULLY_RETURNED", "The full sold quantity has already been returned.");
        }

        return ("ELIGIBLE", null);
    }

    private async Task<EligibilityRuleCheck> EvaluateOriginalReceiptCheckAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        bool requiresReceipt,
        CancellationToken cancellationToken)
    {
        // Deterministic: prefer ISSUED SALE receipts, then newest IssuedAt, then Id.
        var receipt = await _dbContext.Receipts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.SalesOrderId == saleId &&
                        x.OutletId == outletId &&
                        x.ReceiptType == "SALE")
            .OrderByDescending(x => x.ReceiptStatus == "ISSUED")
            .ThenByDescending(x => x.IssuedAt)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.ReceiptNumber,
                x.ReceiptStatus,
                x.ReceiptType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (!requiresReceipt)
        {
            return new EligibilityRuleCheck(
                Status: "NOT_APPLICABLE",
                Passed: true,
                RequiresReview: false,
                Value: "Receipt not required by policy",
                Description: "Selected product return policies do not require an original receipt.",
                Reason: null,
                ReasonCode: null);
        }

        if (receipt is null)
        {
            return new EligibilityRuleCheck(
                Status: "FAILED",
                Passed: false,
                RequiresReview: false,
                Value: "Original receipt missing",
                Description: "An original sale receipt is required by the return policy.",
                Reason: "A valid original receipt is required for this return.",
                ReasonCode: "receipt_required");
        }

        if (receipt.ReceiptStatus is "VOIDED" or "CANCELLED" or "REVERSED")
        {
            return new EligibilityRuleCheck(
                Status: "FAILED",
                Passed: false,
                RequiresReview: false,
                Value: $"Receipt status {receipt.ReceiptStatus}",
                Description: "The original sale receipt is not in an allowable issued state.",
                Reason: "The original receipt is voided, cancelled, or reversed.",
                ReasonCode: "receipt_invalid");
        }

        if (!string.Equals(receipt.ReceiptStatus, "ISSUED", StringComparison.OrdinalIgnoreCase))
        {
            return new EligibilityRuleCheck(
                Status: "FAILED",
                Passed: false,
                RequiresReview: false,
                Value: $"Receipt status {receipt.ReceiptStatus}",
                Description: "The original sale receipt must be issued.",
                Reason: "The original receipt is not issued.",
                ReasonCode: "receipt_invalid");
        }

        return new EligibilityRuleCheck(
            Status: "PASSED",
            Passed: true,
            RequiresReview: false,
            Value: $"Receipt {receipt.ReceiptNumber} verified",
            Description: "Issued original sale receipt verified for the current outlet.",
            Reason: null,
            ReasonCode: null);
    }

    private async Task<EligibilityRuleCheck> EvaluateOriginalPaymentCheckAsync(
        Guid tenantId,
        Guid saleId,
        string saleCurrency,
        CancellationToken cancellationToken)
    {
        var payments = await _dbContext.SalesPayments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == saleId)
            .Select(x => new
            {
                x.PaymentStatus,
                x.PaidAmount,
                x.RefundedAmount,
                x.CurrencyCode
            })
            .ToListAsync(cancellationToken);

        // Successful final settlement states only — do not use display PaymentMethod text.
        var successful = payments
            .Where(x => x.PaymentStatus is "PAID" or "PARTIALLY_REFUNDED")
            .ToList();

        if (successful.Count == 0)
        {
            return new EligibilityRuleCheck(
                Status: "FAILED",
                Passed: false,
                RequiresReview: false,
                Value: "No successful payment found",
                Description: "Original payment settlement could not be verified.",
                Reason: "A successful original payment is required for this return.",
                ReasonCode: "payment_not_verified");
        }

        var currency = (saleCurrency ?? string.Empty).Trim().ToUpperInvariant();
        if (successful.Any(x =>
                !string.Equals(
                    (x.CurrencyCode ?? string.Empty).Trim(),
                    currency,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return new EligibilityRuleCheck(
                Status: "FAILED",
                Passed: false,
                RequiresReview: false,
                Value: "Payment currency mismatch",
                Description: "Successful payment currency must match the original sale currency.",
                Reason: "Original payment currency does not match the sale.",
                ReasonCode: "payment_currency_mismatch");
        }

        var totalPaid = successful.Sum(x => x.PaidAmount);
        if (totalPaid <= 0m)
        {
            return new EligibilityRuleCheck(
                Status: "FAILED",
                Passed: false,
                RequiresReview: false,
                Value: "Payment amount invalid",
                Description: "Successful payment amount must be greater than zero.",
                Reason: "Original payment amount is invalid.",
                ReasonCode: "payment_amount_invalid");
        }

        var tenderCount = successful.Count;
        var value = tenderCount > 1
            ? $"{tenderCount} successful payments verified"
            : "Successful original payment verified";

        return new EligibilityRuleCheck(
            Status: "PASSED",
            Passed: true,
            RequiresReview: false,
            Value: value,
            Description: "Persisted successful payment settlement verified for the original sale.",
            Reason: null,
            ReasonCode: null);
    }

    private static List<PosReturnPolicyCheckDto> BuildSelectedLinePolicyChecks(
        IReadOnlyList<PosReturnSaleLineEligibilityDto> selectedItems,
        IReadOnlyList<ReturnPolicy?> resolvedPolicies,
        EligibilityRuleCheck receiptCheck,
        EligibilityRuleCheck paymentCheck,
        bool requiresInspection,
        bool requiresManagerApproval,
        int windowDays)
    {
        var withinWindowCount = selectedItems.Count(item =>
            item.EligibilityStatus is not "NO_POLICY" and not "WINDOW_EXPIRED");
        var windowPassed = selectedItems.Count > 0 && withinWindowCount == selectedItems.Count;
        var windowDescription = windowDays > 0
            ? $"Within {windowDays} days of purchase"
            : "Return window policy applies to the selected items";

        var policiesResolved = resolvedPolicies.Count(policy => policy is not null);
        var policyPassed = selectedItems.Count > 0 &&
                           policiesResolved == selectedItems.Count &&
                           selectedItems.All(item => item.IsReturnable);
        var blockedCount = selectedItems.Count(item => !item.IsReturnable);

        return
        [
            new(
                "Return Window",
                $"{withinWindowCount} of {selectedItems.Count} selected item(s) are within the return window",
                windowPassed,
                "RETURN_WINDOW",
                windowDescription,
                windowPassed ? "PASSED" : "FAILED",
                windowPassed ? "INFO" : "ERROR",
                windowPassed
                    ? null
                    : "One or more selected items are outside the return window.",
                false),
            new(
                "Original Receipt",
                receiptCheck.Value,
                receiptCheck.Passed,
                "ORIGINAL_RECEIPT",
                receiptCheck.Description,
                receiptCheck.Status,
                receiptCheck.Status == "FAILED" ? "ERROR" : "INFO",
                receiptCheck.Reason,
                false),
            new(
                "Payment Verification",
                paymentCheck.Value,
                paymentCheck.Passed,
                "PAYMENT_VERIFICATION",
                paymentCheck.Description,
                paymentCheck.Status,
                paymentCheck.Status == "FAILED" ? "ERROR" : "INFO",
                paymentCheck.Reason,
                false),
            new(
                "Product Return Policy",
                policyPassed
                    ? $"{policiesResolved} of {selectedItems.Count} selected item(s) resolved an active product return policy"
                    : $"{blockedCount} of {selectedItems.Count} selected item(s) are blocked by product return policy",
                policyPassed,
                "PRODUCT_RETURN_POLICY",
                "Evaluated using product return policy, then tenant default policy (no category policy in schema).",
                policyPassed ? "PASSED" : "FAILED",
                policyPassed ? "INFO" : "ERROR",
                policyPassed
                    ? null
                    : "One or more selected items are not eligible under the resolved return policy.",
                false),
            new(
                "Inspection Requirement",
                requiresInspection
                    ? "Preliminary inspection required by policy"
                    : "No preliminary inspection required by product return policy",
                !requiresInspection,
                "INSPECTION_REQUIRED",
                "Preliminary only. Final inspection may still be required after return-reason selection.",
                requiresInspection ? "REQUIRES_REVIEW" : "NOT_APPLICABLE",
                requiresInspection ? "WARNING" : "INFO",
                requiresInspection
                    ? "Item inspection will be required later in the return flow."
                    : null,
                requiresInspection),
            new(
                "Manager Approval Requirement",
                requiresManagerApproval
                    ? "Manager approval required by return policy"
                    : "Manager approval is not required by return policy",
                !requiresManagerApproval,
                "MANAGER_APPROVAL_REQUIRED",
                "Preliminary policy flag only. Approval is not granted at this step.",
                requiresManagerApproval ? "REQUIRES_REVIEW" : "NOT_APPLICABLE",
                requiresManagerApproval ? "WARNING" : "INFO",
                requiresManagerApproval
                    ? "Manager approval will be required before return completion."
                    : null,
                requiresManagerApproval)
        ];
    }

    private static (
        string OverallStatus,
        bool CanContinue,
        int EligibleItemCount,
        int SelectedItemCount,
        string OverallMessage,
        string? PolicyNote) BuildEligibilitySummary(
        IReadOnlyList<PosReturnSaleLineEligibilityDto> selectedItems,
        IReadOnlyList<PosReturnPolicyCheckDto> checks,
        bool requiresInspection,
        bool requiresManagerApproval,
        IReadOnlyList<string> policyDescriptions,
        int windowDays)
    {
        var selectedCount = selectedItems.Count;
        var eligibleCount = selectedItems.Count(item => item.IsReturnable);
        var hardFailures = checks.Where(check =>
                check.Status is "FAILED" &&
                !check.RequiresReview)
            .ToArray();
        var hasReview = checks.Any(check =>
                check.Status is "REQUIRES_REVIEW" or "UNDER_REVIEW") ||
            requiresInspection ||
            requiresManagerApproval;

        string overallStatus;
        string overallMessage;
        bool canContinue;

        if (eligibleCount == 0)
        {
            overallStatus = "NOT_ELIGIBLE";
            overallMessage =
                "The selected items are not eligible for return under the current policy rules.";
            canContinue = false;
        }
        else if (hardFailures.Length > 0)
        {
            overallStatus = eligibleCount < selectedCount
                ? "PARTIALLY_ELIGIBLE"
                : "NOT_ELIGIBLE";
            overallMessage = hardFailures[0].Reason ??
                             "One or more eligibility checks did not pass.";
            canContinue = false;
        }
        else if (hasReview)
        {
            overallStatus = "ELIGIBLE_WITH_WARNINGS";
            overallMessage =
                "The selected return is eligible to continue, but inspection or manager approval is required later.";
            canContinue = true;
        }
        else
        {
            overallStatus = "ELIGIBLE";
            overallMessage =
                "All policy requirements have been met. You can continue with the return process.";
            canContinue = true;
        }

        string? policyNote = null;
        if (policyDescriptions.Count > 0)
        {
            policyNote = string.Join(" ", policyDescriptions);
        }
        else if (windowDays > 0)
        {
            // Neutral technical summary — never invent packaging/business rules.
            policyNote = $"Return window: {windowDays} days";
        }

        return (
            overallStatus,
            canContinue,
            eligibleCount,
            selectedCount,
            overallMessage,
            policyNote);
    }

    private static string? ResolveImageValue(string? imageUrl, string imageStorageKey) =>
        !string.IsNullOrWhiteSpace(imageUrl)
            ? imageUrl.Trim()
            : string.IsNullOrWhiteSpace(imageStorageKey)
                ? null
                : imageStorageKey.Trim();

    /// <summary>
    /// Rejects values that look like PAN or CVV. Safe payment numbers / short refs are allowed.
    /// </summary>
    private static bool LooksLikeSensitivePaymentSecret(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        var digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length is >= 12 and <= 19)
        {
            return true;
        }

        // Bare 3-digit values look like CVV — never return as a transaction reference.
        return digitsOnly.Length == 3 && trimmed.Length == 3;
    }

    private static decimal RoundAmount(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static bool IsRefundIdempotencyUniqueViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("ux_sales_returns_tenant_idempotency_key", StringComparison.OrdinalIgnoreCase)
            || (message.Contains("idempotency_key", StringComparison.OrdinalIgnoreCase) &&
                message.Contains("sales_returns", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string> GetNextDocumentNumberAsync(
        IQueryable<string> numbersQuery,
        string prefix,
        int paddingLength,
        CancellationToken cancellationToken)
    {
        var existingNumbers = await numbersQuery
            .Where(number => number.StartsWith(prefix))
            .ToListAsync(cancellationToken);
        var max = 0;
        foreach (var number in existingNumbers)
        {
            var suffix = number[prefix.Length..];
            if (int.TryParse(suffix, out var value) && value > max)
            {
                max = value;
            }
        }

        return $"{prefix}{(max + 1).ToString().PadLeft(paddingLength, '0')}";
    }

    private sealed record PaymentAllocationRow(
        SalesPayment Payment,
        Guid PaymentMethodId,
        string MethodCode,
        string PaymentMethodName,
        string MethodType,
        bool SupportsRefund);

    private sealed record PaymentDisplayRow(
        Guid PaymentId,
        Guid SaleId,
        string MethodCode,
        string MethodName,
        string MethodType,
        string? ExternalReference);

    private sealed record PaymentDisplay(
        string Method,
        string? MaskedReference)
    {
        public static PaymentDisplay Empty { get; } = new(string.Empty, null);
    }

    private sealed record SearchSaleRow(
        Guid SaleId,
        string InvoiceNo,
        Guid? CustomerId,
        string CustomerName,
        string Phone,
        DateTimeOffset? SaleDate,
        decimal Total,
        int ItemCount,
        string Currency);

    private sealed record OriginalStockRow(
        Guid SaleLineId,
        Guid InventoryBalanceId,
        decimal SoldQuantity);

    private sealed record PriorRestockRow(
        Guid SaleLineId,
        Guid InventoryBalanceId,
        decimal Quantity);

    private sealed record StockRestorePlan(
        Guid SaleLineId,
        Guid InventoryBalanceId,
        decimal Quantity);

    private sealed record CreditPreviewHeader(
        Guid SaleId,
        string InvoiceNo,
        Guid? CustomerId,
        string CustomerName,
        string CustomerDisplayId,
        DateTimeOffset SaleDate,
        DateTimeOffset IssuedAt,
        string Currency,
        decimal SaleTotal,
        decimal RefundedAmount,
        string PaymentMethod);

    private sealed record CreditPreviewLineRow(
        Guid SaleLineId,
        Guid ProductId,
        string ProductName,
        string? VariantName,
        string? Sku,
        decimal Quantity,
        decimal FulfilledQuantity,
        decimal CancelledQuantity,
        decimal ReturnedQuantity,
        decimal UnitPrice,
        decimal LineSubtotal,
        decimal LineDiscount,
        decimal LineTax,
        Guid? ReturnPolicyId);

    private sealed record EligibilityRuleCheck(
        string Status,
        bool Passed,
        bool RequiresReview,
        string Value,
        string Description,
        string? Reason,
        string? ReasonCode);

    private sealed record EligibilityHeader(
        Guid SaleId,
        string InvoiceNo,
        Guid? CustomerId,
        string CustomerName,
        DateTimeOffset SaleDate,
        string Currency,
        DateTimeOffset IssuedAt);

    private sealed record EligibilityLineRow(
        Guid SaleLineId,
        Guid ProductId,
        Guid? VariantId,
        string ProductName,
        string? VariantName,
        string? Sku,
        decimal Quantity,
        decimal FulfilledQuantity,
        decimal CancelledQuantity,
        decimal ReturnedQuantity,
        decimal UnitPrice,
        decimal LineTotal,
        Guid? ReturnPolicyId);
}
