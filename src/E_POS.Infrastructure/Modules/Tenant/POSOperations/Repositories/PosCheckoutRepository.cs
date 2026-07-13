using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
            var customerExists = await _dbContext.Customers
                .AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == customerId, cancellationToken);

            if (!customerExists)
            {
                return new PosCheckoutCalculationResult("pos_checkout.customer_not_found", null);
            }
        }

        var normalizedLines = request.Lines
            .Where(line => line.VariantId != Guid.Empty && line.Qty > 0)
            .ToList();

        if (normalizedLines.Count == 0)
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
                    product.ProductName,
                    product.IsTaxable))
            .ToListAsync(cancellationToken);

        if (variants.Count != variantIds.Count)
        {
            return new PosCheckoutCalculationResult("pos_checkout.variant_not_found", null);
        }

        var variantsById = variants.ToDictionary(row => row.VariantId);
        var defaultPriceList = await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == ActiveStatus)
            .Select(x => new { x.Id, x.CurrencyCode })
            .FirstOrDefaultAsync(cancellationToken);

        var priceListId = defaultPriceList?.Id;
        var pricesByVariant = new Dictionary<Guid, decimal>();
        if (priceListId.HasValue)
        {
            var priceRows = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PriceListId == priceListId.Value &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == ActiveStatus)
                .Select(x => new { VariantId = x.ProductVariantId!.Value, x.SellingPrice })
                .ToListAsync(cancellationToken);

            pricesByVariant = priceRows.ToDictionary(row => row.VariantId, row => row.SellingPrice);
        }

        var validationMessages = new List<string>();
        decimal subtotal = 0m;
        decimal taxTotal = 0m;
        var itemCount = 0;

        foreach (var line in normalizedLines)
        {
            var variant = variantsById[line.VariantId];
            if (!pricesByVariant.TryGetValue(line.VariantId, out var unitPrice))
            {
                validationMessages.Add($"Price is not configured for {variant.ProductName}.");
                continue;
            }

            var lineSubtotal = unitPrice * line.Qty;
            subtotal += lineSubtotal;
            itemCount += line.Qty;

            if (variant.IsTaxable)
            {
                var taxPercent = await ResolveTaxPercentAsync(
                    tenantId,
                    variant.ProductId,
                    variant.VariantId,
                    now,
                    cancellationToken);
                taxTotal += lineSubtotal * taxPercent / 100m;
            }

            var availableQuantity = await _dbContext.InventoryBalances
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ProductVariantId == line.VariantId)
                .Select(x => (decimal?)x.AvailableQuantity)
                .FirstOrDefaultAsync(cancellationToken);

            if (availableQuantity.HasValue && availableQuantity.Value < line.Qty)
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

        var discount = 0;
        var summary = new PosCheckoutSummaryResponseDto(
            new PosCheckoutBillingSummaryDto(
                itemCount,
                ToMoney(subtotal),
                discount,
                ToMoney(taxTotal),
                ToMoney(subtotal + taxTotal),
                string.IsNullOrWhiteSpace(defaultPriceList?.CurrencyCode)
                    ? "LKR"
                    : defaultPriceList!.CurrencyCode),
            new PosCheckoutSaleDetailsDto(
                FormatSaleType(request.SaleType),
                itemCount,
                now,
                cashierName),
            ResolvePaymentMethods(permissions),
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

        if (request.CustomerId is { } customerId && customerId != Guid.Empty)
        {
            var customerExists = await _dbContext.Customers
                .AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == customerId, cancellationToken);

            if (!customerExists)
            {
                return new PosCheckoutStartPaymentResult("pos_checkout.customer_not_found", null);
            }
        }

        var normalizedLines = request.Lines
            .Where(line => line.VariantId != Guid.Empty && line.Qty > 0)
            .ToList();

        if (normalizedLines.Count == 0)
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.invalid_lines", null);
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
        var defaultPriceList = await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == ActiveStatus)
            .Select(x => new { x.Id, x.CurrencyCode })
            .FirstOrDefaultAsync(cancellationToken);

        var priceListId = defaultPriceList?.Id;
        var currencyCode = string.IsNullOrWhiteSpace(defaultPriceList?.CurrencyCode)
            ? "LKR"
            : defaultPriceList!.CurrencyCode;

        var priceRowsByVariant = new Dictionary<Guid, (decimal SellingPrice, Guid PriceListItemId)>();
        if (priceListId.HasValue)
        {
            var priceRows = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PriceListId == priceListId.Value &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == ActiveStatus)
                .Select(x => new
                {
                    VariantId = x.ProductVariantId!.Value,
                    x.SellingPrice,
                    x.Id
                })
                .ToListAsync(cancellationToken);

            priceRowsByVariant = priceRows.ToDictionary(
                row => row.VariantId,
                row => (row.SellingPrice, row.Id));
        }

        decimal subtotal = 0m;
        decimal taxTotal = 0m;
        var builtLines = new List<BuiltCheckoutLine>(normalizedLines.Count);

        foreach (var line in normalizedLines)
        {
            var variant = variantsById[line.VariantId];
            if (!priceRowsByVariant.TryGetValue(line.VariantId, out var priceRow))
            {
                return new PosCheckoutStartPaymentResult("pos_checkout.price_not_configured", null);
            }

            var quantity = line.Qty;
            var unitPrice = priceRow.SellingPrice;
            var lineSubtotal = unitPrice * quantity;
            var lineTax = 0m;

            if (variant.IsTaxable)
            {
                var taxPercent = await ResolveTaxPercentAsync(
                    tenantId,
                    variant.ProductId,
                    variant.VariantId,
                    now,
                    cancellationToken);
                lineTax = lineSubtotal * taxPercent / 100m;
            }

            var availableQuantity = await _dbContext.InventoryBalances
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ProductVariantId == line.VariantId)
                .Select(x => (decimal?)x.AvailableQuantity)
                .FirstOrDefaultAsync(cancellationToken);

            if (availableQuantity.HasValue && availableQuantity.Value < quantity)
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
                lineTax,
                priceRow.PriceListItemId));
        }

        var discountTotal = 0m;
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
        var paymentMethod = await EnsurePaymentMethodAsync(
            tenantId,
            tenantUserId,
            paymentMethodCode,
            now,
            cancellationToken);

        if (paymentMethod is null)
        {
            return new PosCheckoutStartPaymentResult("pos_checkout.payment_method_not_found", null);
        }

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
                .Where(x => x.TenantId == tenantId && x.Id == selectedCustomerId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

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
            tenantUserId,
            now);

        _dbContext.SalesOrders.Add(salesOrder);

        var responseLines = new List<PosCheckoutStartPaymentLineResponseDto>(builtLines.Count);
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
                builtLine.Variant.ProductName,
                builtLine.Variant.VariantName,
                builtLine.Variant.UomCode,
                builtLine.Variant.UomName,
                builtLine.Variant.ProductType,
                builtLine.Variant.ProductStructure,
                builtLine.Quantity,
                builtLine.UnitPrice,
                builtLine.LineSubtotal,
                builtLine.LineTax,
                now);

            _dbContext.SalesOrderLines.Add(orderLine);
            responseLines.Add(new PosCheckoutStartPaymentLineResponseDto(
                builtLine.Variant.ProductName,
                (int)builtLine.Quantity,
                ToMoney(builtLine.UnitPrice),
                ToMoney(builtLine.LineSubtotal + builtLine.LineTax),
                builtLine.Variant.Sku));

            var inventoryBalance = await _dbContext.InventoryBalances
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId && x.ProductVariantId == builtLine.Variant.VariantId,
                    cancellationToken);

            if (inventoryBalance is not null)
            {
                inventoryBalance.AdjustQuantities(-builtLine.Quantity, 0, 0, 0, now);
            }
        }

        var salesPayment = SalesPayment.CreateCompletedPosPayment(
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
            tenantUserId,
            now);

        _dbContext.SalesPayments.Add(salesPayment);

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

        var businessDate = await _dbContext.TillSessions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == session.SessionId)
            .Select(x => x.BusinessDate)
            .FirstAsync(cancellationToken);

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
        await _dbContext.SaveChangesAsync(cancellationToken);

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

    private async Task<PaymentMethod?> EnsurePaymentMethodAsync(
        Guid tenantId,
        Guid tenantUserId,
        string paymentMethodCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.MethodCode == paymentMethodCode &&
                     x.Status == ActiveStatus,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var defaults = new (string Code, string Name, string Type, bool AllowsChange, int SortOrder)[]
        {
            ("CASH", "Cash", "CASH", true, 0),
            ("CARD", "Card", "CARD", false, 1),
            ("QR", "QR", "QR", false, 2),
            ("SPLIT", "Split", "SPLIT", true, 3)
        };

        foreach (var method in defaults)
        {
            var alreadyExists = await _dbContext.PaymentMethods
                .AnyAsync(
                    x => x.TenantId == tenantId && x.MethodCode == method.Code,
                    cancellationToken);

            if (alreadyExists)
            {
                continue;
            }

            _dbContext.PaymentMethods.Add(PaymentMethod.Create(
                Guid.NewGuid(),
                tenantId,
                method.Code,
                method.Name,
                method.Type,
                true,
                method.AllowsChange,
                method.SortOrder,
                ActiveStatus,
                tenantUserId,
                now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.MethodCode == paymentMethodCode &&
                     x.Status == ActiveStatus,
                cancellationToken);
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

    private async Task<decimal> ResolveTaxPercentAsync(
        Guid tenantId,
        Guid productId,
        Guid variantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.ProductTaxAssignments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ActiveStatus &&
                (x.ProductVariantId == null || x.ProductVariantId == variantId) &&
                (x.AppliesFrom == null || x.AppliesFrom <= now) &&
                (x.AppliesUntil == null || x.AppliesUntil >= now))
            .OrderByDescending(x => x.ProductVariantId != null)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return 0m;
        }

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var rates = await (
                from classRate in _dbContext.TaxClassRates.AsNoTracking()
                join taxRate in _dbContext.TaxRates.AsNoTracking()
                    on classRate.TaxRateId equals taxRate.Id
                where classRate.TenantId == tenantId &&
                      classRate.TaxClassId == assignment.TaxClassId &&
                      taxRate.TenantId == tenantId &&
                      taxRate.Status == ActiveStatus &&
                      (taxRate.ValidFrom == null || taxRate.ValidFrom <= today) &&
                      (taxRate.ValidUntil == null || taxRate.ValidUntil >= today)
                select taxRate.RatePercent)
            .ToListAsync(cancellationToken);

        return rates.Sum();
    }

    private static IReadOnlyList<string> ResolvePaymentMethods(IReadOnlyCollection<string> permissions)
    {
        var methods = new List<string>(4);
        if (permissions.Contains(PaymentPermissions.AcceptCash, StringComparer.Ordinal))
        {
            methods.Add("cash");
        }

        if (permissions.Contains(PaymentPermissions.AcceptCard, StringComparer.Ordinal))
        {
            methods.Add("card");
        }

        if (permissions.Contains(PaymentPermissions.AcceptQr, StringComparer.Ordinal))
        {
            methods.Add("qr");
        }

        if (permissions.Contains(PaymentPermissions.AcceptSplit, StringComparer.Ordinal))
        {
            methods.Add("split");
        }

        return methods;
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

    private sealed record CheckoutVariantRow(
        Guid VariantId,
        Guid ProductId,
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
        decimal LineTax,
        Guid PriceListItemId);
}
