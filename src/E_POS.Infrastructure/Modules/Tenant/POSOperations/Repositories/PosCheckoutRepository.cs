using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Application.Modules.Tenant.Discount.Services;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;

public sealed class PosCheckoutRepository : IPosCheckoutRepository
{
    private const string ActiveStatus = "ACTIVE";

    private readonly EPosDbContext _dbContext;
    private readonly IPosTillSessionRepository _tillSessionRepository;

    public PosCheckoutRepository(
        EPosDbContext dbContext,
        IPosTillSessionRepository tillSessionRepository)
    {
        _dbContext = dbContext;
        _tillSessionRepository = tillSessionRepository;
    }

    public async Task<PosCheckoutCalculationResult> CalculateSummaryAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        PosCheckoutSummaryRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var deviceExists = await _dbContext.PosDevices
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == request.DeviceId, cancellationToken);

        if (!deviceExists)
        {
            return new PosCheckoutCalculationResult("pos_checkout.device_not_found", null);
        }

        var sessionResolution = await _tillSessionRepository.ResolveCurrentSessionAsync(
            tenantId,
            request.DeviceId,
            cancellationToken);

        if (!sessionResolution.IsSuccess)
        {
            return new PosCheckoutCalculationResult(
                sessionResolution.ErrorCode ?? "pos_checkout.till_session_not_open",
                null);
        }

        if (request.CustomerId is { } customerId && customerId != Guid.Empty)
        {
            var customerError = await ValidateCheckoutCustomerAsync(
                tenantId,
                customerId,
                cancellationToken);
            if (customerError is not null)
            {
                return new PosCheckoutCalculationResult(customerError, null);
            }
        }

        var normalizedLines = NormalizeLines(request.Lines);
        if (normalizedLines is null)
        {
            return new PosCheckoutCalculationResult("pos_checkout.invalid_lines", null);
        }

        var variantIds = normalizedLines.Select(line => line.VariantId).Distinct().ToList();
        var variants = await (
                from variant in _dbContext.ProductVariants.AsNoTracking()
                join product in _dbContext.Products.AsNoTracking()
                    on variant.ProductId equals product.Id
                where variant.TenantId == tenantId &&
                      variantIds.Contains(variant.Id) &&
                      variant.Status != ProductConstants.DeletedStatus &&
                      variant.IsSellable &&
                      product.TenantId == tenantId &&
                      product.Status == ProductConstants.ActiveStatus &&
                      product.IsSellable
                select new CheckoutVariantRow(
                    variant.Id,
                    variant.ProductId,
                    variant.SalesUomId,
                    product.ProductName,
                    product.IsTaxable))
            .ToListAsync(cancellationToken);

        if (variants.Count != variantIds.Count)
        {
            return new PosCheckoutCalculationResult("pos_checkout.variant_not_found", null);
        }

        var variantsById = variants.ToDictionary(row => row.VariantId);
        var priceList = await ResolvePriceListAsync(
            tenantId, sessionResolution.Snapshot!.OutletId, now, cancellationToken);
        if (priceList is null)
            return new PosCheckoutCalculationResult("pos_checkout.price_not_configured", null);

        var pricesByVariant = await ResolvePricesAsync(
            tenantId, priceList.Id,
            normalizedLines.Select(line => new PriceLookupInput(
                line.VariantId, variantsById[line.VariantId].ProductId,
                variantsById[line.VariantId].SalesUomId, line.Qty)).ToList(),
            now, cancellationToken);
        if (pricesByVariant.Count != variantIds.Count)
            return new PosCheckoutCalculationResult("pos_checkout.price_not_configured", null);

        var validationMessages = new List<string>();
        decimal subtotal = 0m;
        decimal taxTotal = 0m;
        var itemCount = 0;
        var calculatedLines = new List<CalculatedCheckoutLine>(normalizedLines.Count);
        var availableByVariant = await ResolveAvailableStockAsync(
            tenantId, sessionResolution.Snapshot!.OutletId, variantIds, cancellationToken);
        var taxPercentByVariant = await ResolveTaxPercentsAsync(
            tenantId, variants.Select(x => new TaxLookupInput(x.VariantId, x.ProductId)).ToList(),
            now, cancellationToken);

        foreach (var line in normalizedLines)
        {
            var variant = variantsById[line.VariantId];
            var unitPrice = pricesByVariant[line.VariantId].SellingPrice;

            var grossLineAmount = unitPrice * line.Qty;
            var taxPercent = variant.IsTaxable && taxPercentByVariant.TryGetValue(line.VariantId, out var rate)
                ? rate
                : 0m;
            var lineSubtotal = priceList.PriceIncludesTax && taxPercent > 0m
                ? grossLineAmount * 100m / (100m + taxPercent)
                : grossLineAmount;
            subtotal += lineSubtotal;
            itemCount += line.Qty;
            var lineTax = lineSubtotal * taxPercent / 100m;
            taxTotal += lineTax;
            calculatedLines.Add(new CalculatedCheckoutLine(line.VariantId, lineSubtotal, lineTax));

            if (availableByVariant.TryGetValue(line.VariantId, out var availableQuantity) &&
                availableQuantity < line.Qty)
            {
                validationMessages.Add($"Insufficient stock for {variant.ProductName}.");
            }
        }

        if (itemCount == 0)
        {
            return new PosCheckoutCalculationResult("pos_checkout.invalid_lines", null);
        }

        var cashier = await _dbContext.TenantUsers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == tenantUserId)
            .Select(x => new { x.DisplayName, x.FullName })
            .FirstOrDefaultAsync(cancellationToken);

        var cashierName = cashier?.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(cashierName))
        {
            cashierName = cashier?.FullName?.Trim();
        }

        if (string.IsNullOrWhiteSpace(cashierName))
        {
            cashierName = "Cashier";
        }

        var discountResolution = await ResolveDiscountApplicationAsync(
            tenantId, tenantUserId, request.DeviceId, sessionResolution.Snapshot!.SessionId,
            request.CustomerId, request.SaleType, normalizedLines, request.DiscountApplicationId,
            subtotal, priceList.CurrencyCode,
            now, tracked: false, cancellationToken);
        if (discountResolution.ErrorCode is not null)
        {
            return new PosCheckoutCalculationResult(discountResolution.ErrorCode, null);
        }

        var discountAmount = discountResolution.Application?.DiscountAmountSnapshot ?? 0m;
        if (discountAmount > 0m && subtotal > 0m)
            taxTotal = AllocateDiscountToTax(
                calculatedLines, discountResolution.Application!, discountAmount);
        var discount = ToMoney(discountAmount);
        var summary = new PosCheckoutSummaryResponseDto(
            new PosCheckoutBillingSummaryDto(
                itemCount,
                ToMoney(subtotal),
                discount,
                ToMoney(taxTotal),
                ToMoney(subtotal - discountAmount + taxTotal),
                priceList.CurrencyCode),
            new PosCheckoutSaleDetailsDto(
                FormatSaleType(request.SaleType),
                itemCount,
                now,
                cashierName),
            await ResolvePaymentMethodsAsync(tenantId, permissions, cancellationToken),
            validationMessages);

        return new PosCheckoutCalculationResult(null, summary);
    }

    public async Task<PosCheckoutStartPaymentResult> StartPaymentAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        PosCheckoutStartPaymentRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var deviceExists = await _dbContext.PosDevices
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == request.DeviceId, cancellationToken);

        if (!deviceExists)
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.device_not_found", null);
        }

        var sessionResolution = await _tillSessionRepository.ResolveCurrentSessionAsync(
            tenantId,
            request.DeviceId,
            cancellationToken);

        if (!sessionResolution.IsSuccess || sessionResolution.Snapshot is null)
        {
            return new PosCheckoutStartPaymentResult(
                sessionResolution.ErrorCode ?? "pos_checkout.till_session_not_open",
                null);
        }

        var session = sessionResolution.Snapshot;
        var paymentMethodCode = NormalizePaymentMethodCode(request.PaymentMethod);
        if (paymentMethodCode is null)
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.invalid_payment_method", null);
        }

        if (!HasPaymentPermission(paymentMethodCode, permissions))
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.payment_permission_denied", null);
        }

        if (!string.Equals(paymentMethodCode, "CASH", StringComparison.Ordinal))
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.payment_provider_required", null);
        }

        if (request.CustomerId is { } customerId && customerId != Guid.Empty)
        {
            var customerError = await ValidateCheckoutCustomerAsync(
                tenantId,
                customerId,
                cancellationToken);
            if (customerError is not null)
            {
                return new PosCheckoutStartPaymentResult(customerError, null);
            }
        }

        var normalizedLines = NormalizeLines(request.Lines);
        if (normalizedLines is null)
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.invalid_lines", null);
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length > 100)
            return new PosCheckoutStartPaymentResult("pos_checkout.invalid_idempotency_key", null);
        var idempotencyKey = request.IdempotencyKey.Trim();
        var requestHash = CreatePaymentRequestHash(request, normalizedLines, paymentMethodCode);
        var replay = await ResolveIdempotentPaymentAsync(
            tenantId, idempotencyKey, requestHash, cancellationToken);
        if (replay.Found)
        {
            return replay.Payment is null
                ? new PosCheckoutStartPaymentResult("pos_checkout.idempotency_conflict", null)
                : new PosCheckoutStartPaymentResult(null, replay.Payment);
        }

        var variantIds = normalizedLines.Select(line => line.VariantId).Distinct().ToList();
        var variants = await (
                from variant in _dbContext.ProductVariants.AsNoTracking()
                join product in _dbContext.Products.AsNoTracking()
                    on variant.ProductId equals product.Id
                join uom in _dbContext.UnitOfMeasures.AsNoTracking()
                    on variant.SalesUomId equals uom.Id
                where variant.TenantId == tenantId &&
                      variantIds.Contains(variant.Id) &&
                      variant.Status != ProductConstants.DeletedStatus &&
                      variant.IsSellable &&
                      product.TenantId == tenantId &&
                      product.Status == ProductConstants.ActiveStatus &&
                      product.IsSellable
                select new CheckoutVariantDetailRow(
                    variant.Id,
                    variant.ProductId,
                    product.ProductName,
                    product.ProductType,
                    product.ProductStructure,
                    variant.VariantName,
                    variant.Sku,
                    variant.SalesUomId,
                    uom.UomCode,
                    uom.UomName,
                    product.IsTaxable))
            .ToListAsync(cancellationToken);

        if (variants.Count != variantIds.Count)
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.variant_not_found", null);
        }

        var variantsById = variants.ToDictionary(row => row.VariantId);
        var priceList = await ResolvePriceListAsync(
            tenantId, session.OutletId, now, cancellationToken);
        if (priceList is null)
            return new PosCheckoutStartPaymentResult("pos_checkout.price_not_configured", null);
        var priceListId = priceList.Id;
        var currencyCode = priceList.CurrencyCode;
        var priceRowsByVariant = await ResolvePricesAsync(
            tenantId, priceList.Id,
            normalizedLines.Select(line => new PriceLookupInput(
                line.VariantId, variantsById[line.VariantId].ProductId,
                variantsById[line.VariantId].SalesUomId, line.Qty)).ToList(),
            now, cancellationToken);
        if (priceRowsByVariant.Count != variantIds.Count)
            return new PosCheckoutStartPaymentResult("pos_checkout.price_not_configured", null);

        decimal subtotal = 0m;
        decimal taxTotal = 0m;
        var builtLines = new List<BuiltCheckoutLine>(normalizedLines.Count);
        var availableByVariant = await ResolveAvailableStockAsync(
            tenantId, session.OutletId, variantIds, cancellationToken);
        var taxPercentByVariant = await ResolveTaxPercentsAsync(
            tenantId, variants.Select(x => new TaxLookupInput(x.VariantId, x.ProductId)).ToList(),
            now, cancellationToken);

        foreach (var line in normalizedLines)
        {
            var variant = variantsById[line.VariantId];
            if (!priceRowsByVariant.TryGetValue(line.VariantId, out var priceRow))
            {
                return new PosCheckoutStartPaymentResult("pos_checkout.price_not_configured", null);
            }

            var quantity = line.Qty;
            var listedUnitPrice = priceRow.SellingPrice;
            var taxPercent = variant.IsTaxable && taxPercentByVariant.TryGetValue(line.VariantId, out var rate)
                ? rate
                : 0m;
            var unitPrice = priceList.PriceIncludesTax && taxPercent > 0m
                ? listedUnitPrice * 100m / (100m + taxPercent)
                : listedUnitPrice;
            var lineSubtotal = unitPrice * quantity;
            var lineTax = lineSubtotal * taxPercent / 100m;

            if (availableByVariant.TryGetValue(line.VariantId, out var availableQuantity) &&
                availableQuantity < quantity)
            {
                return new PosCheckoutStartPaymentResult("pos_checkout.insufficient_stock", null);
            }

            subtotal += lineSubtotal;
            taxTotal += lineTax;
            builtLines.Add(new BuiltCheckoutLine(
                line.VariantId,
                variant,
                quantity,
                unitPrice,
                lineSubtotal,
                0m,
                lineTax,
                priceRow.PriceListItemId));
        }

        var discountResolution = await ResolveDiscountApplicationAsync(
            tenantId, tenantUserId, request.DeviceId, session.SessionId,
            request.CustomerId, request.SaleType, normalizedLines, request.DiscountApplicationId,
            subtotal, currencyCode, now, tracked: true, cancellationToken);
        if (discountResolution.ErrorCode is not null)
        {
            return new PosCheckoutStartPaymentResult(discountResolution.ErrorCode, null);
        }

        var discountApplication = discountResolution.Application;
        var discountTotal = discountApplication?.DiscountAmountSnapshot ?? 0m;
        if (discountTotal > 0m && subtotal > 0m)
        {
            var remainingDiscount = discountTotal;
            var eligibleIndexes = builtLines
                .Select((line, index) => new { line, index })
                .Where(x => discountApplication!.DiscountScope == "ORDER" ||
                            x.line.VariantId == discountApplication.TargetProductVariantId)
                .Select(x => x.index)
                .ToList();
            var eligibleSubtotal = eligibleIndexes.Sum(index => builtLines[index].LineSubtotal);
            if (eligibleSubtotal <= 0m)
                return new PosCheckoutStartPaymentResult("pos_checkout.discount_application_invalid", null);
            for (var index = 0; index < builtLines.Count; index++)
            {
                var line = builtLines[index];
                var eligiblePosition = eligibleIndexes.IndexOf(index);
                var lineDiscount = 0m;
                if (eligiblePosition >= 0)
                {
                    lineDiscount = eligiblePosition == eligibleIndexes.Count - 1
                            ? remainingDiscount
                            : Math.Round(discountTotal * line.LineSubtotal / eligibleSubtotal, 4,
                                MidpointRounding.AwayFromZero);
                    lineDiscount = Math.Min(Math.Min(lineDiscount, remainingDiscount), line.LineSubtotal);
                    remainingDiscount -= lineDiscount;
                }

                var adjustedTax = line.LineSubtotal > 0m
                    ? line.LineTax * (line.LineSubtotal - lineDiscount) / line.LineSubtotal
                    : 0m;
                builtLines[index] = line with { LineDiscount = lineDiscount, LineTax = adjustedTax };
            }
            taxTotal = builtLines.Sum(x => x.LineTax);
        }
        var grandTotal = subtotal + taxTotal - discountTotal;

        decimal cashReceived;
        decimal changeDue;
        if (string.Equals(paymentMethodCode, "CASH", StringComparison.Ordinal))
        {
            if (!request.CashReceived.HasValue || request.CashReceived.Value <= 0)
            {
                return new PosCheckoutStartPaymentResult("pos_checkout.cash_received_required", null);
            }

            cashReceived = request.CashReceived.Value;
            if (cashReceived < grandTotal)
            {
                return new PosCheckoutStartPaymentResult("pos_checkout.insufficient_cash", null);
            }

            changeDue = cashReceived - grandTotal;
        }
        else
        {
            cashReceived = grandTotal;
            changeDue = 0m;
        }

        var salesChannelId = await EnsurePosSalesChannelAsync(tenantId, tenantUserId, now, cancellationToken);
        var paymentMethod = await _dbContext.PaymentMethods.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.MethodCode == paymentMethodCode &&
                 x.IsActiveForPos && x.Status == ActiveStatus,
            cancellationToken);

        if (paymentMethod is null)
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.payment_method_not_found", null);
        }


        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var saleId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var orderNumber = await GetNextDocumentNumberAsync(
            tenantId,
            _dbContext.SalesOrders.Where(x => x.TenantId == tenantId).Select(x => x.OrderNumber),
            "SO-",
            6,
            cancellationToken);
        var paymentNumber = await GetNextDocumentNumberAsync(
            tenantId,
            _dbContext.SalesPayments.Where(x => x.TenantId == tenantId).Select(x => x.PaymentNumber),
            "PAY-",
            6,
            cancellationToken);
        var receiptNumber = await GetNextDocumentNumberAsync(
            tenantId,
            _dbContext.Receipts.Where(x => x.TenantId == tenantId).Select(x => x.ReceiptNumber),
            "RCP-",
            6,
            cancellationToken);

        string? customerNameSnapshot = null;
        if (request.CustomerId is { } selectedCustomerId && selectedCustomerId != Guid.Empty)
        {
            customerNameSnapshot = await _dbContext.Customers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.Id == selectedCustomerId &&
                            x.Status == ActiveStatus)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var reportingOutlet = await _dbContext.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == session.OutletId)
            .Select(x => new { x.Id, x.OutletCode, x.OutletName })
            .FirstAsync(cancellationToken);

        var businessDate = await _dbContext.TillSessions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == session.SessionId)
            .Select(x => x.BusinessDate)
            .FirstAsync(cancellationToken);

        var productSnapshotRows = await (
            from product in _dbContext.Products.AsNoTracking()
            join brand in _dbContext.Brands.AsNoTracking()
                on product.BrandId equals brand.Id into brands
            from brand in brands.DefaultIfEmpty()
            join productCategory in _dbContext.ProductCategories.AsNoTracking()
                on product.Id equals productCategory.ProductId into productCategories
            from productCategory in productCategories.Where(x => x.TenantId == tenantId && x.IsPrimaryCategory).DefaultIfEmpty()
            join category in _dbContext.Categories.AsNoTracking()
                on productCategory.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            join parent in _dbContext.Categories.AsNoTracking()
                on category.ParentCategoryId equals parent.Id into parents
            from parent in parents.DefaultIfEmpty()
            join department in _dbContext.Departments.AsNoTracking()
                on category.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            where product.TenantId == tenantId && variantIds.Contains(product.Id) == false &&
                  variants.Select(v => v.ProductId).Contains(product.Id)
            select new
            {
                product.Id,
                BrandName = brand == null ? null : brand.BrandName,
                DepartmentName = department == null ? null : department.DepartmentName,
                CategoryName = category == null || category.ParentCategoryId != null ? parent == null ? category.CategoryName : parent.CategoryName : category.CategoryName,
                SubcategoryName = category == null || category.ParentCategoryId == null ? null : category.CategoryName
            }).ToListAsync(cancellationToken);
        var productSnapshots = productSnapshotRows.ToDictionary(x => x.Id);

        var barcodeSnapshots = await _dbContext.ProductBarcodes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.ProductVariantId.HasValue &&
                        variantIds.Contains(x.ProductVariantId.Value) &&
                        x.IsPrimaryBarcode &&
                        x.Status == ActiveStatus)
            .GroupBy(x => x.ProductVariantId!.Value)
            .Select(g => new { VariantId = g.Key, Barcode = g.Select(x => x.Barcode).FirstOrDefault() })
            .ToDictionaryAsync(x => x.VariantId, x => x.Barcode, cancellationToken);

        var salesOrder = SalesOrder.CreateCompletedPosSale(
            saleId,
            tenantId,
            orderNumber,
            salesChannelId,
            request.CustomerId,
            customerNameSnapshot,
            session.TillId,
            session.SessionId,
            priceListId,
            currencyCode,
            subtotal,
            discountTotal,
            taxTotal,
            grandTotal,
            grandTotal,
            businessDate,
            reportingOutlet.Id,
            reportingOutlet.OutletCode,
            reportingOutlet.OutletName,
            tenantUserId,
            now);

        _dbContext.SalesOrders.Add(salesOrder);

        var responseLines = new List<PosCheckoutStartPaymentLineResponseDto>(builtLines.Count);
        var outletBalances = await (from balance in _dbContext.InventoryBalances
                                    join location in _dbContext.InventoryLocations
                                        on new { balance.TenantId, Id = balance.InventoryLocationId }
                                        equals new { location.TenantId, location.Id }
                                    where balance.TenantId == tenantId && location.OutletId == session.OutletId &&
                                          location.Status == ActiveStatus && location.IsSellableLocation &&
                                          balance.ProductVariantId.HasValue && variantIds.Contains(balance.ProductVariantId.Value)
                                    orderby balance.ProductBatchId, balance.Id
                                    select balance).ToListAsync(cancellationToken);
        var lineNumber = 1;
        foreach (var builtLine in builtLines)
        {
            var orderLine = SalesOrderLine.CreateForPosSale(
                Guid.NewGuid(),
                tenantId,
                saleId,
                lineNumber++,
                builtLine.Variant.ProductId,
                builtLine.Variant.VariantId,
                builtLine.Variant.SalesUomId,
                builtLine.PriceListItemId,
                builtLine.Variant.Sku,
                barcodeSnapshots.GetValueOrDefault(builtLine.Variant.VariantId),
                builtLine.Variant.ProductName,
                builtLine.Variant.VariantName,
                productSnapshots.GetValueOrDefault(builtLine.Variant.ProductId)?.DepartmentName,
                productSnapshots.GetValueOrDefault(builtLine.Variant.ProductId)?.CategoryName,
                productSnapshots.GetValueOrDefault(builtLine.Variant.ProductId)?.SubcategoryName,
                productSnapshots.GetValueOrDefault(builtLine.Variant.ProductId)?.BrandName,
                builtLine.Variant.UomCode,
                builtLine.Variant.UomName,
                builtLine.Variant.ProductType,
                builtLine.Variant.ProductStructure,
                builtLine.Quantity,
                builtLine.UnitPrice,
                builtLine.LineSubtotal,
                builtLine.LineDiscount,
                builtLine.LineTax,
                now);

            _dbContext.SalesOrderLines.Add(orderLine);
            responseLines.Add(new PosCheckoutStartPaymentLineResponseDto(
                builtLine.Variant.ProductName,
                (int)builtLine.Quantity,
                ToMoney(builtLine.UnitPrice),
                ToMoney(builtLine.LineSubtotal - builtLine.LineDiscount + builtLine.LineTax),
                builtLine.Variant.Sku));

            var remainingQuantity = builtLine.Quantity;
            var movementIndex = 0;
            var variantBalances = outletBalances.Where(x =>
                x.ProductVariantId == builtLine.Variant.VariantId).ToList();
            foreach (var inventoryBalance in variantBalances.Where(x => x.AvailableQuantity > 0))
            {
                if (remainingQuantity <= 0) break;
                var quantity = Math.Min(remainingQuantity, inventoryBalance.AvailableQuantity);
                var quantityBefore = inventoryBalance.OnHandQuantity;
                inventoryBalance.AdjustQuantities(-quantity, 0, 0, 0, now);
                var movementId = Guid.NewGuid();
                _dbContext.StockMovements.Add(StockMovement.Create(
                    movementId, tenantId, $"SM-{paymentId:N}-{lineNumber - 1:D3}-{++movementIndex:D3}",
                    inventoryBalance.Id, "SALE", quantityBefore, -quantity, null, null,
                    $"{idempotencyKey}:{lineNumber - 1}:{movementIndex}",
                    $"POS sale {orderNumber}", now, tenantUserId, now));
                _dbContext.StockMovementReferences.Add(StockMovementReference.Create(
                    Guid.NewGuid(), tenantId, movementId, "SALES_ORDER", saleId, orderLine.Id, now));
                remainingQuantity -= quantity;
            }
            if (remainingQuantity > 0)
                return new PosCheckoutStartPaymentResult("pos_checkout.stock_conflict", null);
        }

        var salesPaymentRecords = PosCompletedPaymentPersistence.CreateCash(
            paymentId,
            tenantId,
            saleId,
            paymentNumber,
            paymentMethod.Id,
            session.TillId,
            session.SessionId,
            currencyCode,
            grandTotal,
            string.Equals(paymentMethodCode, "CASH", StringComparison.Ordinal) ? cashReceived : null,
            grandTotal,
            changeDue,
            idempotencyKey,
            requestHash,
            tenantUserId,
            now);

        _dbContext.SalesPayments.Add(salesPaymentRecords.Payment);
        _dbContext.SalesPaymentTransactions.Add(salesPaymentRecords.Transaction);
        _dbContext.SalesPaymentEvents.Add(salesPaymentRecords.Event);

        var receiptDataJson = JsonSerializer.Serialize(new
        {
            saleId,
            orderNumber,
            receiptNumber,
            paymentMethod = request.PaymentMethod.Trim().ToLowerInvariant(),
            items = responseLines.Select(item => new
            {
                name = item.Name,
                qty = item.Qty,
                unitPrice = item.UnitPrice,
                lineTotal = item.LineTotal,
                sku = item.Sku
            })
        });

        var receipt = Receipt.CreateForSale(
            receiptId,
            tenantId,
            receiptNumber,
            saleId,
            session.OutletId,
            session.TillId,
            session.SessionId,
            businessDate,
            tenantUserId,
            currencyCode,
            subtotal,
            discountTotal,
            taxTotal,
            grandTotal,
            grandTotal,
            changeDue,
            receiptDataJson,
            now);

        _dbContext.Receipts.Add(receipt);
        if (discountApplication is not null)
        {
            _dbContext.SalesOrderDiscounts.Add(SalesOrderDiscount.CreateForPosSale(
                Guid.NewGuid(), tenantId, saleId, null, discountApplication.DiscountPolicyId,
                discountApplication.DiscountTypeId, discountApplication.DiscountScope,
                discountApplication.PolicyCodeSnapshot, discountApplication.PolicyNameSnapshot,
                discountApplication.CalculationMethodSnapshot, discountApplication.RequestedValue,
                discountApplication.DiscountAmountSnapshot, discountApplication.RequestReason,
                tenantUserId, now));
            var previousStatus = discountApplication.ApplicationStatus;
            discountApplication.MarkApplied(saleId, now);
            _dbContext.PosDiscountApplicationEvents.Add(PosDiscountApplicationEvent.Record(
                Guid.NewGuid(), tenantId, discountApplication.Id, "APPLIED", previousStatus,
                "APPLIED", tenantUserId, $"Applied to sale {orderNumber}.", now));
        }
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return new PosCheckoutStartPaymentResult("pos_checkout.stock_conflict", null);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return new PosCheckoutStartPaymentResult("pos_checkout.idempotency_conflict", null);
        }

        var response = new PosCheckoutStartPaymentResponseDto(
            saleId,
            saleId,
            orderNumber,
            receiptNumber,
            receiptNumber,
            ToMoney(subtotal),
            ToMoney(discountTotal),
            ToMoney(taxTotal),
            ToMoney(grandTotal),
            ToMoney(cashReceived),
            ToMoney(changeDue),
            request.PaymentMethod.Trim().ToLowerInvariant(),
            currencyCode,
            "completed",
            "completed",
            now,
            paymentId,
            responseLines);

        return new PosCheckoutStartPaymentResult(null, response);
    }

    private async Task<string?> ValidateCheckoutCustomerAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var status = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == customerId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (status is null)
        {
            return "pos_checkout.customer_not_found";
        }

        return status.Trim().ToUpperInvariant() switch
        {
            ActiveStatus => null,
            "INACTIVE" => "pos_checkout.customer_inactive",
            "BLOCKED" => "pos_checkout.customer_blocked",
            "DELETED" => "pos_checkout.customer_deleted",
            _ => "pos_checkout.customer_not_eligible"
        };
    }

    private async Task<Guid> EnsurePosSalesChannelAsync(
        Guid tenantId,
        Guid tenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingId = await (from s in _dbContext.SalesChannels.AsNoTracking()
                                join p in _dbContext.PlatformSalesChannels.AsNoTracking() on s.PlatformSalesChannelId equals p.Id
                                where s.TenantId == tenantId && p.ChannelType == "PHYSICAL" && s.Status == ActiveStatus
                                orderby s.SortOrder
                                select (Guid?)s.Id).FirstOrDefaultAsync(cancellationToken);

        if (existingId.HasValue)
        {
            return existingId.Value;
        }

        var platformChannelId = await _dbContext.PlatformSalesChannels
            .AsNoTracking()
            .Where(x => x.ChannelCode == "POS")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!platformChannelId.HasValue)
        {
            var newPlatformChannel = E_POS.Domain.Modules.Platform.PlatformFoundation.Entities.PlatformSalesChannel.Create(
                Guid.NewGuid(),
                "POS",
                "Point of Sale",
                "PHYSICAL",
                now);
            _dbContext.PlatformSalesChannels.Add(newPlatformChannel);
            platformChannelId = newPlatformChannel.Id;
        }

        var channel = SalesChannel.Create(
            Guid.NewGuid(),
            tenantId,
            platformChannelId.Value,
            "POS",
            ActiveStatus,
            0,
            now);

        _dbContext.SalesChannels.Add(channel);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return channel.Id;
    }


    private static async Task<string> GetNextDocumentNumberAsync(
        Guid tenantId,
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

    private static string? NormalizePaymentMethodCode(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return null;
        }

        return paymentMethod.Trim().ToUpperInvariant() switch
        {
            "CASH" => "CASH",
            "CARD" => "CARD",
            "QR" => "QR",
            "SPLIT" => "SPLIT",
            _ => null
        };
    }

    private static bool HasPaymentPermission(string paymentMethodCode, IReadOnlyCollection<string> permissions)
    {
        return paymentMethodCode switch
        {
            "CASH" => permissions.Contains(PaymentPermissions.AcceptCash, StringComparer.Ordinal),
            "CARD" => permissions.Contains(PaymentPermissions.AcceptCard, StringComparer.Ordinal),
            "QR" => permissions.Contains(PaymentPermissions.AcceptQr, StringComparer.Ordinal),
            "SPLIT" => permissions.Contains(PaymentPermissions.AcceptSplit, StringComparer.Ordinal),
            _ => false
        };
    }

    private async Task<ResolvedPriceList?> ResolvePriceListAsync(
        Guid tenantId, Guid outletId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var posChannelId = await (from sc in _dbContext.SalesChannels.AsNoTracking()
                                  join psc in _dbContext.PlatformSalesChannels.AsNoTracking() on sc.PlatformSalesChannelId equals psc.Id
                                  where sc.TenantId == tenantId && psc.ChannelType == "POS" && sc.Status == ActiveStatus
                                  orderby sc.SortOrder
                                  select (Guid?)sc.Id)
                                  .FirstOrDefaultAsync(cancellationToken);

        var candidates = await _dbContext.PriceLists.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == ActiveStatus &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .Select(x => new ResolvedPriceList(
                x.Id, x.CurrencyCode, x.PriceIncludesTax, x.IsDefaultPriceList, x.Priority,
                _dbContext.PriceListOutlets.Any(mapping =>
                    mapping.TenantId == tenantId && mapping.PriceListId == x.Id &&
                    mapping.OutletId == outletId && mapping.Status == ActiveStatus),
                posChannelId.HasValue && _dbContext.PriceListChannels.Any(mapping =>
                    mapping.TenantId == tenantId && mapping.PriceListId == x.Id &&
                    mapping.SalesChannelId == posChannelId.Value && mapping.Status == ActiveStatus)))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(x => x.OutletMatched || x.ChannelMatched || x.IsDefault)
            .OrderByDescending(x => x.OutletMatched && x.ChannelMatched)
            .ThenByDescending(x => x.OutletMatched)
            .ThenByDescending(x => x.ChannelMatched)
            .ThenByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .FirstOrDefault();
    }

    private async Task<Dictionary<Guid, ResolvedPrice>> ResolvePricesAsync(
        Guid tenantId, Guid priceListId, IReadOnlyList<PriceLookupInput> inputs,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var productIds = inputs.Select(x => x.ProductId).Distinct().ToList();
        var variantIds = inputs.Select(x => x.VariantId).ToList();
        var rows = await _dbContext.PriceListItems.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PriceListId == priceListId &&
                        x.Status == ActiveStatus && productIds.Contains(x.ProductId) &&
                        (!x.ProductVariantId.HasValue || variantIds.Contains(x.ProductVariantId.Value)) &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .Select(x => new PriceRow(x.Id, x.ProductId, x.ProductVariantId, x.UomId,
                x.SellingPrice, x.MinQuantity))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, ResolvedPrice>();
        foreach (var input in inputs)
        {
            var row = rows
                .Where(x => x.ProductId == input.ProductId &&
                            (!x.ProductVariantId.HasValue || x.ProductVariantId == input.VariantId) &&
                            (!x.UomId.HasValue || x.UomId == input.UomId) &&
                            x.MinQuantity <= input.Quantity)
                .OrderByDescending(x => x.ProductVariantId.HasValue)
                .ThenByDescending(x => x.UomId.HasValue)
                .ThenByDescending(x => x.MinQuantity)
                .ThenBy(x => x.Id)
                .FirstOrDefault();
            if (row is not null)
                result[input.VariantId] = new ResolvedPrice(row.SellingPrice, row.Id);
        }
        return result;
    }

    private async Task<Dictionary<Guid, decimal>> ResolveAvailableStockAsync(
        Guid tenantId, Guid outletId, IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken) =>
        await (from balance in _dbContext.InventoryBalances.AsNoTracking()
               join location in _dbContext.InventoryLocations.AsNoTracking()
                   on new { balance.TenantId, Id = balance.InventoryLocationId }
                   equals new { location.TenantId, location.Id }
               where balance.TenantId == tenantId && location.OutletId == outletId &&
                     location.Status == ActiveStatus && location.IsSellableLocation &&
                     balance.ProductVariantId.HasValue && variantIds.Contains(balance.ProductVariantId.Value)
               group balance by balance.ProductVariantId!.Value into grouped
               select new { VariantId = grouped.Key, Available = grouped.Sum(x => x.AvailableQuantity) })
            .ToDictionaryAsync(x => x.VariantId, x => x.Available, cancellationToken);

    private async Task<Dictionary<Guid, decimal>> ResolveTaxPercentsAsync(
        Guid tenantId, IReadOnlyList<TaxLookupInput> inputs, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var productIds = inputs.Select(x => x.ProductId).Distinct().ToList();
        var variantIds = inputs.Select(x => x.VariantId).ToList();
        var assignments = await _dbContext.ProductTaxAssignments.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == ActiveStatus &&
                        productIds.Contains(x.ProductId) &&
                        (!x.ProductVariantId.HasValue || variantIds.Contains(x.ProductVariantId.Value)) &&
                        (!x.AppliesFrom.HasValue || x.AppliesFrom <= now) &&
                        (!x.AppliesUntil.HasValue || x.AppliesUntil >= now))
            .Select(x => new { x.ProductId, x.ProductVariantId, x.TaxClassId, x.AppliesFrom })
            .ToListAsync(cancellationToken);
        var classIds = assignments.Select(x => x.TaxClassId).Distinct().ToList();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var rates = await (from classRate in _dbContext.TaxClassRates.AsNoTracking()
                           join taxRate in _dbContext.TaxRates.AsNoTracking()
                               on new { classRate.TenantId, Id = classRate.TaxRateId }
                               equals new { taxRate.TenantId, taxRate.Id }
                           where classRate.TenantId == tenantId && classRate.Status == ActiveStatus &&
                                 classIds.Contains(classRate.TaxClassId) && taxRate.Status == ActiveStatus &&
                                 (!taxRate.ValidFrom.HasValue || taxRate.ValidFrom <= today) &&
                                 (!taxRate.ValidUntil.HasValue || taxRate.ValidUntil >= today)
                           select new TaxRateRow(classRate.TaxClassId, classRate.SortOrder,
                               taxRate.RatePercent, taxRate.IsCompound))
            .ToListAsync(cancellationToken);

        var effectiveByClass = rates.GroupBy(x => x.TaxClassId).ToDictionary(
            group => group.Key,
            group => group.OrderBy(x => x.SortOrder).Aggregate(0m, (effective, rate) =>
                effective + (rate.IsCompound
                    ? (100m + effective) * rate.RatePercent / 100m
                    : rate.RatePercent)));
        var result = new Dictionary<Guid, decimal>();
        foreach (var input in inputs)
        {
            var assignment = assignments
                .Where(x => x.ProductId == input.ProductId &&
                            (!x.ProductVariantId.HasValue || x.ProductVariantId == input.VariantId))
                .OrderByDescending(x => x.ProductVariantId.HasValue)
                .ThenByDescending(x => x.AppliesFrom)
                .FirstOrDefault();
            if (assignment is not null && effectiveByClass.TryGetValue(assignment.TaxClassId, out var rate))
                result[input.VariantId] = rate;
        }
        return result;
    }

    private async Task<IReadOnlyList<string>> ResolvePaymentMethodsAsync(
        Guid tenantId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        var configuredCodes = await _dbContext.PaymentMethods
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActiveForPos && x.Status == ActiveStatus)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.MethodCode)
            .ToListAsync(cancellationToken);

        return configuredCodes
            .Where(code => HasPaymentPermission(code, permissions))
            .Select(code => code.ToLowerInvariant())
            .ToList();
    }

    private static string FormatSaleType(string? saleType)
    {
        if (string.IsNullOrWhiteSpace(saleType))
        {
            return "New Sale";
        }

        return string.Equals(saleType.Trim(), "NewSale", StringComparison.OrdinalIgnoreCase)
            ? "New Sale"
            : saleType.Trim();
    }

    private static int ToMoney(decimal value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static decimal AllocateDiscountToTax(
        IReadOnlyList<CalculatedCheckoutLine> lines,
        PosDiscountApplication application,
        decimal discountAmount)
    {
        var eligible = lines
            .Where(line => application.DiscountScope == "ORDER" ||
                           line.VariantId == application.TargetProductVariantId)
            .ToList();
        var eligibleSubtotal = eligible.Sum(x => x.Subtotal);
        if (eligibleSubtotal <= 0m) return lines.Sum(x => x.Tax);

        var remaining = discountAmount;
        decimal adjustedTaxTotal = 0m;
        foreach (var line in lines)
        {
            var eligibleIndex = eligible.FindIndex(x => x == line);
            var lineDiscount = 0m;
            if (eligibleIndex >= 0)
            {
                lineDiscount = eligibleIndex == eligible.Count - 1
                    ? remaining
                    : Math.Round(discountAmount * line.Subtotal / eligibleSubtotal, 4,
                        MidpointRounding.AwayFromZero);
                lineDiscount = Math.Min(Math.Min(lineDiscount, remaining), line.Subtotal);
                remaining -= lineDiscount;
            }
            adjustedTaxTotal += line.Subtotal > 0m
                ? line.Tax * (line.Subtotal - lineDiscount) / line.Subtotal
                : 0m;
        }
        return adjustedTaxTotal;
    }

    private sealed record CheckoutVariantRow(
        Guid VariantId,
        Guid ProductId,
        Guid SalesUomId,
        string ProductName,
        bool IsTaxable);

    private sealed record CheckoutVariantDetailRow(
        Guid VariantId,
        Guid ProductId,
        string ProductName,
        string ProductType,
        string ProductStructure,
        string VariantName,
        string? Sku,
        Guid SalesUomId,
        string UomCode,
        string UomName,
        bool IsTaxable);

    private sealed record BuiltCheckoutLine(
        Guid VariantId,
        CheckoutVariantDetailRow Variant,
        decimal Quantity,
        decimal UnitPrice,
        decimal LineSubtotal,
        decimal LineDiscount,
        decimal LineTax,
        Guid PriceListItemId);

    private sealed record ResolvedPriceList(
        Guid Id, string CurrencyCode, bool PriceIncludesTax, bool IsDefault,
        int Priority, bool OutletMatched, bool ChannelMatched);
    private sealed record PriceLookupInput(Guid VariantId, Guid ProductId, Guid UomId, int Quantity);
    private sealed record PriceRow(Guid Id, Guid ProductId, Guid? ProductVariantId,
        Guid? UomId, decimal SellingPrice, decimal MinQuantity);
    private sealed record ResolvedPrice(decimal SellingPrice, Guid PriceListItemId);
    private sealed record TaxLookupInput(Guid VariantId, Guid ProductId);
    private sealed record TaxRateRow(Guid TaxClassId, int SortOrder, decimal RatePercent, bool IsCompound);
    private sealed record CalculatedCheckoutLine(Guid VariantId, decimal Subtotal, decimal Tax);
    private sealed record IdempotentPaymentResolution(
        bool Found, PosCheckoutStartPaymentResponseDto? Payment);

    private async Task<DiscountApplicationResolution> ResolveDiscountApplicationAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid deviceId,
        Guid tillSessionId,
        Guid? customerId,
        string? saleType,
        IReadOnlyList<PosCheckoutLineRequestDto> lines,
        Guid? applicationId,
        decimal subtotal,
        string currencyCode,
        DateTimeOffset now,
        bool tracked,
        CancellationToken cancellationToken)
    {
        if (!applicationId.HasValue || applicationId == Guid.Empty)
        {
            return new(null, null);
        }

        IQueryable<PosDiscountApplication> query = _dbContext.PosDiscountApplications;
        if (!tracked) query = query.AsNoTracking();
        var application = await query.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == applicationId.Value,
            cancellationToken);
        if (application is null) return new("pos_checkout.discount_application_not_found", null);
        if (application.RequestedByTenantUserId != tenantUserId || application.PosDeviceId != deviceId ||
            application.TillSessionId != tillSessionId)
            return new("pos_checkout.discount_context_mismatch", null);
        if (!application.CanBeUsed(now))
            return new(application.ApplicationStatus == "PENDING_APPROVAL"
                ? "pos_checkout.discount_approval_required"
                : "pos_checkout.discount_application_invalid", null);

        var policyStillActive = await _dbContext.DiscountPolicies.AsNoTracking().AnyAsync(x =>
            x.TenantId == tenantId && x.Id == application.DiscountPolicyId && x.Status == ActiveStatus &&
            (!x.StartsAt.HasValue || x.StartsAt <= now) && (!x.EndsAt.HasValue || x.EndsAt >= now),
            cancellationToken);
        if (!policyStillActive) return new("pos_checkout.discount_policy_inactive", null);

        var snapshotJson = PosDiscountCartFingerprint.CreateSnapshotJson(
            deviceId, saleType, customerId, lines, ToMoney(subtotal), currencyCode);
        var cartHash = PosDiscountCartFingerprint.Hash(snapshotJson);
        if (!string.Equals(cartHash, application.CartHash, StringComparison.Ordinal))
            return new("pos_checkout.discount_cart_changed", null);
        if (application.DiscountAmountSnapshot > subtotal)
            return new("pos_checkout.discount_application_invalid", null);

        return new(null, application);
    }

    private static List<PosCheckoutLineRequestDto>? NormalizeLines(
        IReadOnlyList<PosCheckoutLineRequestDto>? lines)
    {
        if (lines is null || lines.Count == 0 ||
            lines.Any(line => line.VariantId == Guid.Empty || line.Qty <= 0))
        {
            return null;
        }

        try
        {
            return lines
                .GroupBy(line => line.VariantId)
                .OrderBy(group => group.Key)
                .Select(group => new PosCheckoutLineRequestDto(
                    group.Key,
                    checked(group.Sum(line => line.Qty))))
                .ToList();
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static string CreatePaymentRequestHash(
        PosCheckoutStartPaymentRequestDto request,
        IReadOnlyList<PosCheckoutLineRequestDto> normalizedLines,
        string paymentMethodCode)
    {
        var json = JsonSerializer.Serialize(new
        {
            request.DeviceId,
            saleType = string.IsNullOrWhiteSpace(request.SaleType) ? "NewSale" : request.SaleType.Trim(),
            request.CustomerId,
            lines = normalizedLines.Select(x => new { x.VariantId, x.Qty }),
            paymentMethod = paymentMethodCode,
            request.CashReceived,
            request.DiscountApplicationId
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private async Task<IdempotentPaymentResolution> ResolveIdempotentPaymentAsync(
        Guid tenantId, string idempotencyKey, string requestHash,
        CancellationToken cancellationToken)
    {
        var payment = await _dbContext.SalesPayments.AsNoTracking().FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey,
            cancellationToken);
        if (payment is null) return new(false, null);
        if (!string.Equals(payment.PaymentNote, $"POS_REQUEST_HASH:{requestHash}", StringComparison.Ordinal))
            return new(true, null);

        var order = await _dbContext.SalesOrders.AsNoTracking().FirstAsync(
            x => x.TenantId == tenantId && x.Id == payment.SalesOrderId, cancellationToken);
        var receiptNumber = await _dbContext.Receipts.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == order.Id)
            .Select(x => x.ReceiptNumber).FirstAsync(cancellationToken);
        var methodCode = await _dbContext.PaymentMethods.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == payment.PaymentMethodId)
            .Select(x => x.MethodCode).FirstAsync(cancellationToken);
        var lines = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == order.Id)
            .OrderBy(x => x.LineNumber)
            .Select(x => new PosCheckoutStartPaymentLineResponseDto(
                x.ProductNameSnapshot, (int)x.Quantity, ToMoney(x.UnitPrice),
                ToMoney(x.LineTotalAmount), x.SkuSnapshot))
            .ToListAsync(cancellationToken);

        return new(true, new PosCheckoutStartPaymentResponseDto(
            order.Id, order.Id, order.OrderNumber, receiptNumber, receiptNumber,
            ToMoney(order.SubtotalAmount), ToMoney(order.DiscountAmount), ToMoney(order.TaxAmount),
            ToMoney(order.TotalAmount), ToMoney(payment.TenderedAmount ?? payment.PaidAmount),
            ToMoney(payment.ChangeAmount), methodCode.ToLowerInvariant(), order.CurrencyCode,
            "completed", "completed", payment.PaidAt ?? payment.InitiatedAt, payment.Id, lines));
    }

    private sealed record DiscountApplicationResolution(
        string? ErrorCode,
        PosDiscountApplication? Application);
}
