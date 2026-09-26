using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Catalog.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace E_POS.UnitTests.TenantAdminReports
{
    public class ReportingExportPopulatedDataTests
    {
        private DbContextOptions<EPosDbContext> CreateOptions(string dbName) =>
            new DbContextOptionsBuilder<EPosDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

        private T Create<T>(params (string, object)[] props) where T : new()
        {
            var obj = new T();
            foreach (var p in props)
            {
                var type = typeof(T);
                PropertyInfo? propInfo = null;
                while (type != null && propInfo == null)
                {
                    propInfo = type.GetProperty(p.Item1, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                    type = type.BaseType;
                }
                if (propInfo != null)
                {
                    propInfo.SetValue(obj, p.Item2);
                }
            }
            return obj;
        }

        [Fact]
        public async Task ExportSchemas_ContainPopulatedData_AndMatchRepositoryKeys()
        {
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var outletId = Guid.NewGuid();
            var tillId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var returnId = Guid.NewGuid();
            var options = CreateOptions(Guid.NewGuid().ToString());

            await using (var db = new EPosDbContext(options))
            {
                var tenant = Create<Tenant>(("Id", tenantId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow), ("CurrencyCode", "USD"), ("Timezone", "UTC"));
                db.Tenants.Add(tenant);
                db.TenantUsers.Add(Create<TenantUser>(("Id", userId), ("TenantId", tenantId), ("AccountStatus", "ACTIVE"), ("OutletAccessScope", "ALL_OUTLETS"), ("TillAccessScope", "ALL_ACCESSIBLE_TILLS"), ("UpdatedAt", DateTimeOffset.UtcNow), ("StaffCode", "TEST")));
                db.Outlets.Add(Create<Outlet>(("Id", outletId), ("TenantId", tenantId), ("OutletName", "Test Outlet"), ("Status", "ACTIVE"), ("UpdatedAt", DateTimeOffset.UtcNow)));
                db.Tills.Add(Create<Till>(("Id", tillId), ("TenantId", tenantId), ("OutletId", outletId), ("TillName", "Test Till"), ("Status", "ACTIVE")));
                db.SalesChannels.Add(Create<SalesChannel>(("Id", tenantId), ("TenantId", tenantId), ("CustomName", "POS Channel"), ("UpdatedAt", DateTimeOffset.UtcNow)));
                db.InventoryLocations.Add(Create<InventoryLocation>(("Id", locationId), ("TenantId", tenantId), ("OutletId", outletId), ("LocationName", "Storefront")));
                db.Products.Add(Create<Product>(("Id", productId), ("TenantId", tenantId), ("ProductName", "Test Product")));
                
                db.SalesOrders.Add(Create<SalesOrder>(("Id", orderId), ("TenantId", tenantId), ("OrderNumber", "ORD-123"),
                    ("ReportingOutletId", outletId), ("ReportingOutletNameSnapshot", "Test Outlet"), ("TillId", tillId), ("OrderStatus", "COMPLETED"), ("PaymentStatus", "PAID"), ("FulfillmentStatus", "FULFILLED"), ("UpdatedAt", DateTimeOffset.UtcNow), ("BusinessDate", DateTimeOffset.UtcNow.Date), ("CompletedAt", DateTimeOffset.UtcNow), ("SalesChannelId", tenantId), ("SubtotalAmount", 100m), ("TaxAmount", 10m), ("TotalAmount", 110m), ("PaidAmount", 110m)));
                
                db.SalesOrderLines.Add(Create<SalesOrderLine>(("Id", Guid.NewGuid()), ("TenantId", tenantId), ("SalesOrderId", orderId), ("ProductId", productId), ("ProductNameSnapshot", "Test Product"), ("Quantity", 2), ("ReturnedQuantity", 0), ("LineSubtotalAmount", 100m), ("LineTaxAmount", 10m)));
                
                var pmId = Guid.NewGuid();
                db.PaymentMethods.Add(Create<PaymentMethod>(("Id", pmId), ("TenantId", tenantId), ("MethodName", "Cash")));
                db.SalesPayments.Add(Create<SalesPayment>(("Id", paymentId), ("TenantId", tenantId), ("SalesOrderId", orderId), ("PaymentMethodId", pmId), ("PaymentStatus", "PAID"), ("PaidAmount", 110m), ("RequestedAmount", 110m), ("TenderedAmount", 110m), ("PaidAt", DateTimeOffset.UtcNow)));
                
                db.TillSessions.Add(Create<TillSession>(("Id", Guid.NewGuid()), ("TenantId", tenantId), ("TillId", tillId), ("OutletId", outletId), ("SessionStatus", "CLOSED"), ("OpenedAt", DateTimeOffset.UtcNow.AddHours(-1)), ("ClosedAt", DateTimeOffset.UtcNow)));
                
                var balanceId = Guid.NewGuid();
                db.InventoryBalances.Add(Create<InventoryBalance>(("Id", balanceId), ("TenantId", tenantId), ("InventoryLocationId", locationId), ("ProductId", productId), ("AvailableQuantity", 50), ("OnHandQuantity", 50)));
                db.StockMovements.Add(Create<StockMovement>(("Id", Guid.NewGuid()), ("TenantId", tenantId), ("InventoryBalanceId", balanceId), ("ProductId", productId), ("InventoryLocationId", locationId), ("MovementType", "SALE"), ("MovedQuantity", 2), ("OccurredAt", DateTimeOffset.UtcNow)));
                
                db.SalesOrderTaxes.Add(Create<SalesOrderTax>(("Id", Guid.NewGuid()), ("TenantId", tenantId), ("SalesOrderId", orderId), ("TaxNameSnapshot", "GST"), ("TaxAmount", 10m), ("TaxableAmount", 100m)));

                db.SalesReturns.Add(Create<SalesReturn>(("Id", returnId), ("TenantId", tenantId), ("ReturnNumber", "RET-123"), ("OriginalSalesOrderId", orderId), ("ProcessingOutletId", outletId), ("ReturnStatus", "COMPLETED"), ("RefundStatus", "REFUNDED")));
                
                await db.SaveChangesAsync();
            }

            await using (var db = new EPosDbContext(options))
            {
                var repo = new TenantAdminReportsRepository(db);
                var context = new TenantRequestContext(tenantId, userId, new List<string> { "tenant.reports.sales.view", "tenant.reports.export", "tenant.inventory.stock-value.view", "tenant.reports.outlets.view", "tenant.reports.stock.view" });

                var clockMock = new Mock<IDateTimeProvider>();
                clockMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

                var reports = new[]
                {
                    ("sales", "transactions", "orderNumber"),
                    ("sales", "channels", "salesChannelName"),
                    ("sales", "tax", "taxAmount"),
                    ("sales", "payments", "paymentMethodName"),
                    ("sales", "payment-transactions", "paymentMethodName"),
                    ("outlets", "tills", "tillName"),
                    ("sales", "online", "orderNumber"),
                    ("sales", "collections", "orderNumber"),
                    ("sales", "returns", "returnNumber"),
                    ("sales", "products", "productName"),
                    ("stock", "current", "productName"),
                    ("stock", "movements", "productName")
                };

                var failures = new List<string>();

                foreach (var (type, section, expectedKey) in reports)
                {
                    try
                    {
                        var req = new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, section, 1, 100, null, null);
                        ReportResultDto res;
                        if (type == "sales") res = await repo.GetSalesAsync(context, req, CancellationToken.None);
                        else if (type == "stock") res = await repo.GetStockAsync(context, req, CancellationToken.None);
                        else res = await repo.GetOutletsAsync(context, req, CancellationToken.None);

                        var records = res.Records;
                        if (records == null || records.Count == 0)
                        {
                            failures.Add($"[{section}] Repo returned no records");
                            continue;
                        }

                        var record = records[0];
                        if (!record.ContainsKey(expectedKey) || record[expectedKey] == null || string.IsNullOrWhiteSpace(record[expectedKey]?.ToString()))
                        {
                            failures.Add($"[{section}] Expected key '{expectedKey}' not populated in repo result");
                            continue;
                        }

                        var exportReq = new ReportExportRequest(type, section, "csv", req);
                        var csvBytes = CsvGenerator.Generate(exportReq, res, context, clockMock.Object);
                        var csv = Encoding.UTF8.GetString(csvBytes);
                        
                        var lines = csv.Split('\n');
                        var headerLine = lines.FirstOrDefault(l => l.Contains(expectedKey));
                        if (headerLine == null)
                        {
                            failures.Add($"[{section}] CSV header missing canonical key '{expectedKey}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"[{section}] Exception: {ex.Message}");
                    }
                }

                if (failures.Count > 0)
                {
                    throw new Exception("Populated data check failed:\n" + string.Join("\n", failures));
                }
            }
        }
    }
}
