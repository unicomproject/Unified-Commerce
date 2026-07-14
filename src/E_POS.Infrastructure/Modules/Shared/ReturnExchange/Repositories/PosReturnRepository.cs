using System.Data;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Shared.Refund.Entities;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Repositories;

public sealed class PosReturnRepository : IPosReturnRepository
{
    private readonly EPosDbContext _dbContext;

    public PosReturnRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PosReturnCompleteRepositoryResult> CompleteReturnAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosReturnCompleteCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

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
            Guid.NewGuid(),
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
                "NOT_CAPTURED"),
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
                receipt.IssuedAt,
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
            return null;
        }

        var lineRows = await (
            from line in _dbContext.SalesOrderLines.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on line.ProductId equals product.Id
            where line.TenantId == tenantId &&
                  line.SalesOrderId == saleId &&
                  product.TenantId == tenantId
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
            images.TryGetValue(row.ProductId, out var imageStorageKey);
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
                reason));
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
            header.PaymentMethod,
            string.Empty,
            header.Currency,
            items,
            checks);
    }

    public async Task<PosReturnSaleSearchPageDto> SearchOriginalSalesAsync(
        Guid tenantId,
        string searchType,
        string? search,
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
                  order.OrderType == "POS_SALE" &&
                  order.Status == "COMPLETED" &&
                  (order.PaymentStatus == "PAID" ||
                   order.PaymentStatus == "PARTIALLY_REFUNDED") &&
                  receipt.ReceiptType == "SALE" &&
                  receipt.ReceiptStatus == "ISSUED"
            select new { Order = order, Receipt = receipt, Customer = customer };

        if (searchType != "recent")
        {
            var pattern = $"%{search}%";
            query = searchType switch
            {
                "invoice" => query.Where(x =>
                    EF.Functions.ILike(x.Receipt.ReceiptNumber, pattern) ||
                    EF.Functions.ILike(x.Order.OrderNumber, pattern)),
                "sale" => query.Where(x =>
                    EF.Functions.ILike(x.Order.OrderNumber, pattern)),
                "mobile" => query.Where(x =>
                    (x.Order.CustomerPhoneSnapshot != null &&
                     EF.Functions.ILike(x.Order.CustomerPhoneSnapshot, pattern)) ||
                    (x.Customer != null && x.Customer.Phone != null &&
                     EF.Functions.ILike(x.Customer.Phone, pattern)) ||
                    (x.Customer != null && x.Customer.NormalizedPhone != null &&
                     EF.Functions.ILike(x.Customer.NormalizedPhone, pattern))),
                "customer" => query.Where(x =>
                    (x.Order.CustomerNameSnapshot != null &&
                     EF.Functions.ILike(x.Order.CustomerNameSnapshot, pattern)) ||
                    (x.Customer != null && EF.Functions.ILike(x.Customer.Name, pattern))),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Receipt.IssuedAt)
            .ThenByDescending(x => x.Order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PosReturnSaleSummaryDto(
                x.Order.Id,
                x.Receipt.ReceiptNumber,
                x.Order.CustomerId,
                x.Order.CustomerNameSnapshot ??
                    (x.Customer != null ? x.Customer.Name : string.Empty) ?? string.Empty,
                x.Order.CustomerPhoneSnapshot ??
                    (x.Customer != null ? x.Customer.Phone : null) ?? string.Empty,
                (from payment in _dbContext.SalesPayments
                 join method in _dbContext.PaymentMethods
                     on payment.PaymentMethodId equals method.Id
                 where payment.TenantId == tenantId &&
                       method.TenantId == tenantId &&
                       payment.SalesOrderId == x.Order.Id &&
                       (payment.PaymentStatus == "PAID" ||
                        payment.PaymentStatus == "PARTIALLY_REFUNDED")
                 orderby payment.PaidAt
                 select method.MethodName).FirstOrDefault() ?? string.Empty,
                string.Empty,
                x.Order.CompletedAt ?? x.Receipt.IssuedAt,
                x.Receipt.TotalAmount,
                _dbContext.SalesOrderLines.Count(line =>
                    line.TenantId == tenantId && line.SalesOrderId == x.Order.Id),
                x.Receipt.CurrencyCode))
            .ToListAsync(cancellationToken);

        return new PosReturnSaleSearchPageDto(items, page, pageSize, totalCount);
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

    private static string? ResolveImageValue(string? imageUrl, string imageStorageKey) =>
        !string.IsNullOrWhiteSpace(imageUrl)
            ? imageUrl.Trim()
            : string.IsNullOrWhiteSpace(imageStorageKey)
                ? null
                : imageStorageKey.Trim();

    private static decimal RoundAmount(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

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

    private sealed record EligibilityHeader(
        Guid SaleId,
        string InvoiceNo,
        Guid? CustomerId,
        string CustomerName,
        DateTimeOffset SaleDate,
        string Currency,
        DateTimeOffset IssuedAt,
        string PaymentMethod);

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
