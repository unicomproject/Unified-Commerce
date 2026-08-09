using System.Reflection;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.Customer;

public sealed class PosCustomerRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsCustomerAndMakesNormalizedContactsDiscoverable()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var repository = new PosCustomerRepository(dbContext);
        var customer = CustomerEntity.CreatePosCustomer(
            Guid.NewGuid(),
            tenantId,
            "CUS000001",
            "Kamal Perera",
            "+94 77-123-4567",
            "Kamal@Example.com",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var added = await repository.AddAsync(customer, CancellationToken.None);

        Assert.True(added);
        Assert.True(await repository.NormalizedPhoneExistsAsync(
            tenantId,
            "+94771234567",
            CancellationToken.None));
        Assert.True(await repository.NormalizedEmailExistsAsync(
            tenantId,
            "KAMAL@EXAMPLE.COM",
            CancellationToken.None));
        var persisted = await dbContext.Customers.SingleAsync(x => x.Id == customer.Id);
        Assert.Equal("POS", persisted.SourceType);
        Assert.Equal("ACTIVE", persisted.Status);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyActiveTenantCustomersWithPaginationOrderedByRecentlyAdded()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        var baseTime = DateTimeOffset.UtcNow;
        dbContext.Customers.Add(CreateCustomer(tenantId, "CUS-001", "Alice", "ACTIVE", baseTime.AddMinutes(-20)));
        dbContext.Customers.Add(CreateCustomer(tenantId, "CUS-002", "Bob", "ACTIVE", baseTime.AddMinutes(-10)));
        dbContext.Customers.Add(CreateCustomer(tenantId, "CUS-003", "Charlie", "ACTIVE", baseTime));
        dbContext.Customers.Add(CreateCustomer(tenantId, "CUS-004", "David", "INACTIVE", baseTime));
        dbContext.Customers.Add(CreateCustomer(otherTenantId, "CUS-001", "Other Tenant", "ACTIVE", baseTime));
        await dbContext.SaveChangesAsync();

        var repository = new PosCustomerRepository(dbContext);
        var page1 = await repository.ListAsync(
            tenantId,
            null,
            null,
            null,
            1,
            2,
            CancellationToken.None);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal("Charlie", page1.Items[0].FullName);
        Assert.Equal("Bob", page1.Items[1].FullName);

        var page2 = await repository.ListAsync(
            tenantId,
            null,
            null,
            null,
            2,
            2,
            CancellationToken.None);

        Assert.Equal(2, page2.Page);
        var customer = Assert.Single(page2.Items);
        Assert.Equal("Alice", customer.FullName);
    }

    [Fact]
    public async Task GetOrdersAsync_ReturnsOutletAndTillNamesFromExistingRelationships()
    {
        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Development Main Store", "DEV", "ACTIVE",
            "RETAIL", "UTC", true, null, null, null, now));
        dbContext.Tills.Add(Till.Create(
            tillId, tenantId, outletId, "Front Till 01", "Front", 1,
            "TILL-01", "FIXED", 0, "LKR", true, "ACTIVE", null, now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            Guid.NewGuid(), tenantId, "SO-000001", Guid.NewGuid(), customerId,
            "Customer", tillId, Guid.NewGuid(), null, "LKR", false, 2800, 0,
            0, 2800, 2800, null, now));
        await dbContext.SaveChangesAsync();

        var result = await new PosCustomerRepository(dbContext).GetOrdersAsync(
            tenantId, customerId, 1, 20, null, null, null,
            CancellationToken.None);

        var order = Assert.Single(result.Items);
        Assert.Equal("Development Main Store", order.OutletDisplayName);
        Assert.Equal("Front Till 01", order.TillName);
    }

    private static CustomerEntity CreateCustomer(
        Guid tenantId,
        string customerCode,
        string name,
        string status,
        DateTimeOffset? createdAt = null)
    {
        var created = createdAt ?? DateTimeOffset.UtcNow;
        var customer = new CustomerEntity();
        Set(customer, "Id", Guid.NewGuid());
        Set(customer, "TenantId", tenantId);
        Set(customer, "CustomerCode", customerCode);
        Set(customer, "Name", name);
        Set(customer, "Phone", "+94770000000");
        Set(customer, "NormalizedPhone", "+94770000000");
        Set(customer, "SourceType", "POS");
        Set(customer, "Status", status);
        Set(customer, "CreatedAt", created);
        Set(customer, "UpdatedAt", created);
        return customer;
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var property = entity.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(entity, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
